using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BruteForceCracker
{
    public partial class MainWindow : Window
    {
        private readonly PasswordHasher _hasher = new PasswordHasher();
        private readonly BruteForceEngine _engine = new BruteForceEngine();
        
        private CancellationTokenSource _cts;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            _timer.Tick += (s, e) => LblTime.Text = _stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff");
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            int length = Random.Shared.Next(4, 6); 
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            
            char[] passwordArray = new char[length];
            for (int i = 0; i < length; i++) 
            {
                passwordArray[i] = chars[Random.Shared.Next(chars.Length)];
            }

            TxtPlain.Text = new string(passwordArray);
            TxtHash.Text = _hasher.ComputeHash(TxtPlain.Text);
            TxtResult.Text = "";
            TxtLog.Clear();
            LblStatus.Text = $"Būsena: Sugeneruotas {length} simbolių tikslas. Galite pradėti ataką.";
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtHash.Text))
            {
                MessageBox.Show("Pirmiausia sugeneruokite pradinį slaptažodį!", "Klaida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnStart.IsEnabled = false;
            BtnStop.IsEnabled = true;
            TxtResult.Text = "Ieškoma...";
            TxtLog.Text = "Paleidžiama daugiagijė paieška...\n";
            
            _cts = new CancellationTokenSource();
            _stopwatch.Restart();
            _timer.Start();

            string targetHash = TxtHash.Text;
            string foundPassword = null;
            long multiThreadTime = 0;

            await Task.Run(() =>
            {
                foundPassword = _engine.BuildMultiThread(targetHash, (currentCombination) =>
                {
                    Dispatcher.Invoke(() => LblStatus.Text = $"Progresas (Daugiagijis): Tikrinama [{currentCombination}]");
                }, _cts.Token);
            });

            _stopwatch.Stop();
            _timer.Stop();
            multiThreadTime = _stopwatch.ElapsedMilliseconds;

            if (_cts.IsCancellationRequested)
            {
                EndAttack("Būsena: Ataka nutraukta vartotojo.");
                TxtResult.Text = "STOPPED";
                return;
            }

            TxtResult.Text = foundPassword ?? "Slaptažodis nerastas";
            TxtLog.Text += $"-> Daugiagijis režimas: {multiThreadTime} ms\n\n";

            if (foundPassword != null)
            {
                TxtLog.Text += "Paleidžiamas viengijis režimas palyginimui...\n";
                LblStatus.Text = "Būsena: Vykdomas viengijis našumo testas...";
                
                _stopwatch.Restart();
                
                await Task.Run(() => _engine.BuildSingleThread(targetHash, _cts.Token));
                
                _stopwatch.Stop();
                long singleThreadTime = _stopwatch.ElapsedMilliseconds;

                TxtLog.Text += $"-> Viengijis režimas: {singleThreadTime} ms\n";
                double acceleration = (double)singleThreadTime / Math.Max(1, multiThreadTime);
                TxtLog.Text += $"[*] Daugiagijis veikė {acceleration:F2}x kartų greičiau!";
            }

            EndAttack("Būsena: Ataka sėkmingai baigta.");
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
        }

        private void EndAttack(string statusMessage)
        {
            _timer.Stop();
            _stopwatch.Stop();
            BtnStart.IsEnabled = true;
            BtnStop.IsEnabled = false;
            LblStatus.Text = statusMessage;
        }
    }
}