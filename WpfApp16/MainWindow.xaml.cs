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

        // Обработчик выбора файла через диалоговое окно
        private void ChooseFile_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            if (fileDialog.ShowDialog() == true)
            {
                filePathTextBox.Text = fileDialog.FileName;
            }
        }

        // Обработчик кнопки "Выполнить"
        private async void Execute_Click(object sender, RoutedEventArgs e)
        {
            string selectedFilePath = filePathTextBox.Text;
            string password = passwordBox.Password;

            // Проверка на наличие файла и пароля
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

            // Инициализация отмены и интерфейса
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

        // Метод для обработки файла асинхронно
        private async Task ProcessFileAsync(string filePath, string key, IProgress<int> progress, CancellationToken token)
        {
            byte[] fileData = await File.ReadAllBytesAsync(filePath, token);
            byte[] encryptionKey = Encoding.UTF8.GetBytes(key);
            int fileSize = fileData.Length;

            // Моделируем задержку для процесса шифрования
            int delayStep = 100;
            int totalSteps = 100; // Количество шагов для отображения прогресса

            await Task.Run(async () =>
            {
                for (int step = 0; step < totalSteps; step++)
                {
                    token.ThrowIfCancellationRequested();

                    int startIdx = (step * fileSize) / totalSteps;
                    int endIdx = ((step + 1) * fileSize) / totalSteps;

                    // Симуляция шифрования данных
                    for (int i = startIdx; i < endIdx; i++)
                    {
                        fileData[i] ^= encryptionKey[i % encryptionKey.Length];
                    }

                    // Отчёт прогресса
                    progress.Report((step * 100) / totalSteps);
                    await Task.Delay(delayStep, token);
                }

                // Завершаем запись в файл
                await File.WriteAllBytesAsync(filePath, fileData, token);
            }, token);
        }

        // Метод для отмены текущей операции
        private void CancelOperation_Click(object sender, RoutedEventArgs e)
        {
            cancellationSource?.Cancel();
        }

        // Вспомогательный метод для отображения сообщений
        private void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }

        // Вспомогательный метод для сброса интерфейса
        private void ResetUI()
        {
            filePathTextBox.Text = string.Empty;
            passwordBox.Password = string.Empty;
            progressIndicator.Value = 0;
        }

        // Вспомогательный метод для изменения состояния элементов интерфейса
        private void ToggleUIState(bool isProcessing)
        {
            executeButton.IsEnabled = !isProcessing;
            cancelButton.IsEnabled = isProcessing;
            progressIndicator.IsIndeterminate = isProcessing;
        }
    }
}
