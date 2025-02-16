using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WpfApp16
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationSource;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChooseFile_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == true)
            {
                filePathTextBox.Text = fileDialog.FileName;
            }
        }


        private async void Execute_Click(object sender, RoutedEventArgs e)
        {
            string selectedFilePath = filePathTextBox.Text;
            string password = passwordBox.Password;

            if (string.IsNullOrWhiteSpace(selectedFilePath) || !File.Exists(selectedFilePath))
            {
                ShowMessage("Выберите правильный файл!");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                ShowMessage("Укажите пароль!");
                return;
            }

            cancellationSource = new CancellationTokenSource();
            ToggleUIState(true);

            var progress = new Progress<int>(percent => progressIndicator.Value = percent);
            try
            {
                await ProcessFileAsync(selectedFilePath, password, progress, cancellationSource.Token);
                ShowMessage("Операция завершена!");
                ResetUI();
            }
            catch (OperationCanceledException)
            {
                ShowMessage("Операция была отменена");
                ResetUI();
            }
            catch (Exception ex)
            {
                ShowMessage($"Произошла ошибка: {ex.Message}");
            }
            finally
            {
                ToggleUIState(false);
            }
        }

        private async Task ProcessFileAsync(string filePath, string key, IProgress<int> progress, CancellationToken token)
        {
            byte[] fileData = await File.ReadAllBytesAsync(filePath, token);
            byte[] encryptionKey = Encoding.UTF8.GetBytes(key);
            int fileSize = fileData.Length;

            int delayStep = 100;
            int totalSteps = 100; 

            await Task.Run(async () =>
            {
                for (int step = 0; step < totalSteps; step++)
                {
                    token.ThrowIfCancellationRequested();

                    int startIdx = (step * fileSize) / totalSteps;
                    int endIdx = ((step + 1) * fileSize) / totalSteps;


                    for (int i = startIdx; i < endIdx; i++)
                    {
                        fileData[i] ^= encryptionKey[i % encryptionKey.Length];
                    }

                    progress.Report((step * 100) / totalSteps);
                    await Task.Delay(delayStep, token);
                }

                await File.WriteAllBytesAsync(filePath, fileData, token);
            }, token);
        }

        private void CancelOperation_Click(object sender, RoutedEventArgs e)
        {
            cancellationSource?.Cancel();
        }

        private void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }

        private void ResetUI()
        {
            filePathTextBox.Text = string.Empty;
            passwordBox.Password = string.Empty;
            progressIndicator.Value = 0;
        }

        private void ToggleUIState(bool isProcessing)
        {
            executeButton.IsEnabled = !isProcessing;
            cancelButton.IsEnabled = isProcessing;
            progressIndicator.IsIndeterminate = isProcessing;
        }
    }
}
