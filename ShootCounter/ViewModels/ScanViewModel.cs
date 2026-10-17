using ShootCounter.TargetScan;
using SkiaSharp;
using System.Diagnostics;

namespace ShootCounter.ViewModels
{
    // one hit on the scan page, detected or placed by hand
    public class ScanHit
    {
        public SKPoint Position { get; set; }
        public int Score { get; set; }
        // no daylight through the hole, the shooter has to confirm it
        public bool Uncertain { get; set; }
        // so close to a ring line that the score may be off by one
        public bool Borderline { get; set; }
        public bool Manual { get; set; }
    }

    public class ScanViewModel : BaseViewModel
    {
        private readonly TaskCompletionSource<IReadOnlyList<int>?> _completion = new();
        private readonly ScanTarget _target;
        private readonly int _expectedShots;
        private readonly bool _allowMoreShots;
        private readonly List<ScanHit> _hits = [];
        private TargetScanResult? _result;
        private string _statusText = "Scheibe wird ausgewertet …";
        private string _summaryText = string.Empty;
        private bool _hasResult;

        public Command AcceptCommand { get; }
        public Command CancelCommand { get; }

        // raised whenever the picture has to be redrawn
        public event EventHandler? Changed;

        public SKBitmap? Photo { get; private set; }
        public IReadOnlyList<ScanHit> Hits => _hits;
        public TargetScanResult? Result => _result;

        // the values a hit can have on this target, lowest first (0 = miss)
        public IReadOnlyList<int> Valuations { get; }

        // the scores of the accepted hits, or null if the scan was cancelled
        public Task<IReadOnlyList<int>?> Completion => _completion.Task;

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string SummaryText
        {
            get => _summaryText;
            set => SetProperty(ref _summaryText, value);
        }

        public bool HasResult
        {
            get => _hasResult;
            set
            {
                SetProperty(ref _hasResult, value);
                AcceptCommand.ChangeCanExecute();
            }
        }

        // expectedShots: bullets of the run, allowMoreShots: the run can take more bullets than that
        public ScanViewModel(ScanTarget target, IEnumerable<int> valuations, int expectedShots, bool allowMoreShots)
        {
            _target = target;
            Valuations = valuations.OrderBy(n => n).ToList();
            _expectedShots = expectedShots;
            _allowMoreShots = allowMoreShots;
            Title = "Scheibe scannen";
            AcceptCommand = new Command(OnAccept, () => HasResult);
            CancelCommand = new Command(() => _completion.TrySetResult(null));
        }

        public async Task AnalyzeAsync(Stream photo)
        {
            IsBusy = true;
            try
            {
                var (bitmap, result) = await Task.Run(() => TargetScanner.Scan(photo, _target));

                Photo = bitmap;
                _result = result;

                if (result == null)
                {
                    StatusText = _target switch
                    {
                        ScanTarget.Ordnance25m => "Keine Scheibe erkannt. Fotografiere die ganze Scheibe möglichst gerade von vorne.",
                        ScanTarget.Morgarten => "Keine Figur erkannt. Fotografiere die ganze grüne Figur möglichst gerade von vorne.",
                        _ => "Keine Scheibe erkannt. Der dunkle Spiegel und die Ringe darum müssen gut sichtbar sein.",
                    };
                }
                else
                {
                    _hits.AddRange(result.Hits.Select(n => new ScanHit
                    {
                        Position = n.Image,
                        Score = n.Score,
                        Uncertain = n.Uncertain,
                        Borderline = n.Borderline,
                    }));
                    HasResult = true;
                    UpdateTexts();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusText = "Das Foto konnte nicht ausgewertet werden.";
            }
            finally
            {
                IsBusy = false;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        // the hit at an image position, if any; the tolerance makes small holes easy to tap
        public ScanHit? HitAt(SKPoint position, float tolerance)
        {
            return _hits
                .Select(n => (hit: n, distance: SKPoint.Distance(n.Position, position)))
                .Where(n => n.distance <= tolerance)
                .OrderBy(n => n.distance)
                .Select(n => n.hit)
                .FirstOrDefault();
        }

        public void AddHit(SKPoint position)
        {
            if (_result == null)
                return;

            _hits.Add(new ScanHit { Position = position, Score = _result.ScoreAt(position), Manual = true });
            UpdateTexts();
        }

        public void RemoveHit(ScanHit hit)
        {
            _hits.Remove(hit);
            UpdateTexts();
        }

        public void ConfirmHit(ScanHit hit)
        {
            hit.Uncertain = false;
            hit.Borderline = false;
            UpdateTexts();
        }

        public void SetScore(ScanHit hit, int score)
        {
            hit.Score = score;
            hit.Uncertain = false;
            hit.Borderline = false;
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            var scores = _hits.Select(n => n.Score).OrderByDescending(n => n).ToList();
            SummaryText = scores.Count == 0
                ? "Keine Treffer"
                : $"{scores.Count} von {_expectedShots} Schuss: {string.Join(" ", scores)} = {scores.Sum()}";

            int uncertain = _hits.Count(n => n.Uncertain);
            int borderline = _hits.Count(n => n.Borderline);
            StatusText = uncertain > 0
                ? $"{uncertain} unsichere(r) Treffer (orange): bitte bestätigen oder löschen."
                : borderline > 0
                    ? $"{borderline} Linienschuss/-schüsse (gelb): bitte Wertung prüfen."
                    : "Treffer antippen zum Ändern, freie Stelle antippen zum Hinzufügen.";

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private async void OnAccept()
        {
            if (_hits.Any(n => n.Uncertain))
            {
                await Shell.Current.DisplayAlert("Unsichere Treffer", "Bitte bestätige oder lösche zuerst die orange markierten Treffer.", "OK");
                return;
            }

            if (!_allowMoreShots && _hits.Count > _expectedShots)
            {
                await Shell.Current.DisplayAlert("Zu viele Treffer", $"Die Passe hat {_expectedShots} Schuss. Bitte lösche die überzähligen Treffer.", "OK");
                return;
            }

            _completion.TrySetResult(_hits.Select(n => n.Score).OrderByDescending(n => n).ToList());
        }
    }
}
