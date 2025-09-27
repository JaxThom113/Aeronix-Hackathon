using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Aspose.Words;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MyApp;

public partial class MainWindow : Window
{
    private string? _selectedFilePath;
    private string? _convertedFilePath;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void BrowseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select Word Document",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Word Documents")
                    {
                        Patterns = new[] { "*.doc", "*.docx" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            };

            var files = await StorageProvider.OpenFilePickerAsync(options);
            
            if (files.Count > 0)
            {
                _selectedFilePath = files[0].Path.LocalPath;
                FilePathTextBox.Text = Path.GetFileName(_selectedFilePath);
                ConvertButton.IsEnabled = true;
                
                // Hide previous results
                ProgressBorder.IsVisible = false;
                ResultBorder.IsVisible = false;
                DownloadPanel.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error selecting file: {ex.Message}");
        }
    }

    private async void ConvertButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedFilePath) || !File.Exists(_selectedFilePath))
        {
            await ShowErrorDialog("Please select a valid Word document first.");
            return;
        }

        try
        {
            // Show progress
            ProgressBorder.IsVisible = true;
            ResultBorder.IsVisible = false;
            DownloadPanel.IsVisible = false;
            ConvertButton.IsEnabled = false;

            // Perform conversion
            await ConvertWordToPdfAsync(_selectedFilePath);
        }
        catch (Exception ex)
        {
            ProgressBorder.IsVisible = false;
            await ShowErrorDialog($"Conversion failed: {ex.Message}");
        }
        finally
        {
            ConvertButton.IsEnabled = true;
        }
    }

    private async Task ConvertWordToPdfAsync(string inputPath)
    {
        try
        {
            // Generate output path
            var directory = Path.GetDirectoryName(inputPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var fileName = Path.GetFileNameWithoutExtension(inputPath);
            _convertedFilePath = Path.Combine(directory, $"{fileName}_converted.pdf");

            // Perform conversion on background thread to avoid blocking UI
            await Task.Run(() =>
            {
                // Load the Word document
                var doc = new Document(inputPath);

                // Save as PDF
                doc.Save(_convertedFilePath, SaveFormat.Pdf);
            });

            // Hide progress and show results
            ProgressBorder.IsVisible = false;
            ResultBorder.IsVisible = true;
            DownloadPanel.IsVisible = true;
            
            ResultText.Text = $"PDF saved to: {Path.GetFileName(_convertedFilePath)}";
        }
        catch (Exception ex)
        {
            ProgressBorder.IsVisible = false;
            throw new Exception($"Failed to convert document: {ex.Message}");
        }
    }

    private async void DownloadButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_convertedFilePath) || !File.Exists(_convertedFilePath))
        {
            await ShowErrorDialog("No converted file available for download.");
            return;
        }

        try
        {
            var options = new FilePickerSaveOptions
            {
                Title = "Save PDF As",
                SuggestedFileName = Path.GetFileName(_convertedFilePath),
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PDF Files")
                    {
                        Patterns = new[] { "*.pdf" }
                    }
                }
            };

            var file = await StorageProvider.SaveFilePickerAsync(options);
            
            if (file != null)
            {
                // Copy the converted file to the selected location
                File.Copy(_convertedFilePath, file.Path.LocalPath, overwrite: true);
                await ShowInfoDialog($"PDF saved successfully to: {file.Path.LocalPath}");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error saving file: {ex.Message}");
        }
    }

    private async void OpenFolderButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_convertedFilePath) || !File.Exists(_convertedFilePath))
        {
            await ShowErrorDialog("No converted file available.");
            return;
        }

        try
        {
            var folderPath = Path.GetDirectoryName(_convertedFilePath);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error opening folder: {ex.Message}");
        }
    }

    private async Task ShowErrorDialog(string message)
    {
        var dialog = new Window
        {
            Title = "Error",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        okButton.Click += (s, e) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 0, 0, 20)
                },
                okButton
            }
        };

        await dialog.ShowDialog(this);
    }

    private async Task ShowInfoDialog(string message)
    {
        var dialog = new Window
        {
            Title = "Success",
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        okButton.Click += (s, e) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 0, 0, 20)
                },
                okButton
            }
        };

        await dialog.ShowDialog(this);
    }
}