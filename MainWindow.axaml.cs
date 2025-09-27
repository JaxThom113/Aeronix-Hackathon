using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Aspose.Words;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.Threading;

namespace MyApp;

public partial class MainWindow : Window
{
    private List<string> _selectedFilePaths = new List<string>();
    private List<string> _convertedFilePaths = new List<string>();
    private System.Threading.CancellationTokenSource? _loadingCts;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenConverter_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var welcome = this.FindControl<Grid>("WelcomeGrid");
            var converter = this.FindControl<Grid>("ConverterGrid");
            if (welcome != null) welcome.IsVisible = false;
            if (converter != null) converter.IsVisible = true;
        }
        catch { }
    }

    private void BackToWelcome_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var welcome = this.FindControl<Grid>("WelcomeGrid");
            var converter = this.FindControl<Grid>("ConverterGrid");
            if (welcome != null) welcome.IsVisible = true;
            if (converter != null) converter.IsVisible = false;
        }
        catch { }
    }

    private void DisplaySelectedFiles()
    {
        try
        {
            FileListPanel.Children.Clear();

            if (_selectedFilePaths == null || _selectedFilePaths.Count == 0)
            {
                FilePathTextBox.Text = "No files selected";
                ConvertButton.IsEnabled = false;
                return;
            }

            // Create file items
            foreach (var filePath in _selectedFilePaths)
            {
                var fileName = Path.GetFileName(filePath);
                var fileItem = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Margin = new Avalonia.Thickness(0, 2)
                };

                // File icon
                fileItem.Children.Add(new TextBlock
                {
                    Text = "📄",
                    FontSize = 14,
                    Margin = new Avalonia.Thickness(0, 0, 8, 0),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                });

                // File name
                fileItem.Children.Add(new TextBlock
                {
                    Text = fileName,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Avalonia.Thickness(0, 0, 8, 0)
                });

                // Remove button
                var removeButton = new Button
                {
                    Content = "✕",
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    Width = 20,
                    Height = 20,
                    Padding = new Avalonia.Thickness(0),
                    Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    Foreground = new SolidColorBrush(Colors.White),
                    CornerRadius = new Avalonia.CornerRadius(10),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Tag = filePath
                };
                removeButton.Click += (sender, e) => RemoveFileByPath(filePath);
                fileItem.Children.Add(removeButton);
                FileListPanel.Children.Add(fileItem);
            }

            FilePathTextBox.Text = $"{_selectedFilePaths.Count} file(s) selected";
            ConvertButton.IsEnabled = _selectedFilePaths.Count > 0;
            AdjustScrollViewerHeight();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error displaying files: {ex.Message}");
            FilePathTextBox.Text = "Error displaying files";
            ConvertButton.IsEnabled = false;
        }
    }

    private void ClearFileDisplay()
    {
        FileListPanel.Children.Clear();
        FilePathTextBox.Text = "No files selected";
        AdjustScrollViewerHeight();
    }

    private void AdjustScrollViewerHeight()
    {
        try
        {
            if (_selectedFilePaths == null || _selectedFilePaths.Count == 0)
            {
                // No files - use minimum height
                FileScrollViewer.Height = 60;
            }
            else if (_selectedFilePaths.Count <= 3)
            {
                // 1-3 files - compact height
                FileScrollViewer.Height = 60 + (_selectedFilePaths.Count * 25);
            }
            else if (_selectedFilePaths.Count <= 6)
            {
                // 4-6 files - medium height
                FileScrollViewer.Height = 120 + ((_selectedFilePaths.Count - 3) * 25);
            }
            else
            {
                // 7+ files - maximum height with scroll
                FileScrollViewer.Height = 200;
            }
        }
        catch (Exception ex)
        {
            // Fallback to default height if there's an error
            FileScrollViewer.Height = 100;
            System.Diagnostics.Debug.WriteLine($"Error adjusting scroll viewer height: {ex.Message}");
        }
    }

    private void RemoveFileByPath(string filePath)
    {
        try
        {
            if (!string.IsNullOrEmpty(filePath) && _selectedFilePaths.Contains(filePath))
            {
                _selectedFilePaths.Remove(filePath);
                DisplaySelectedFiles(); // Refresh the display
                
                // Hide previous results when files are removed
                ResetUIState();
            }
        }
        catch (Exception ex)
        {
            // Log error or show user-friendly message
            System.Diagnostics.Debug.WriteLine($"Error removing file: {ex.Message}");
        }
    }

    private async void BrowseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Select Word Documents",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("All Files")
                    {
                        // Allow all file instead of just Word Files, so Netlists and BOMs are included
                        Patterns = new[] { "*.*" }
                    }
                }
            };

            var files = await StorageProvider.OpenFilePickerAsync(options);
            
            if (files.Count > 0)
            {
                _selectedFilePaths.Clear();
                foreach (var file in files)
                {
                    _selectedFilePaths.Add(file.Path.LocalPath);
                }
                
                // Display selected files in individual items
                DisplaySelectedFiles();
                ConvertButton.IsEnabled = true;
                
                // Hide previous results and reset UI state
                ResetUIState();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error selecting files: {ex.Message}");
        }
    }

    private async void ConvertButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_selectedFilePaths.Count == 0 || _selectedFilePaths.Any(path => !File.Exists(path)))
        {
            await ShowErrorDialog("Please select valid documents first.");
            return;
        }

        try
        {
            // Show progress and start loading animation
            ProgressBorder.IsVisible = true;
            ProgressBar.IsVisible = true;
            ProgressBar.IsIndeterminate = true; // let the control show a loading marquee
            // Start face loading animation loop
            _loadingCts?.Cancel();
            _loadingCts = new System.Threading.CancellationTokenSource();
            ResultBorder.IsVisible = false;
            DownloadPanel.IsVisible = false;
            ConvertButton.IsEnabled = false;

            // Perform conversion for all files
            await CreateTestProcedures(_selectedFilePaths);
        }
        catch (Exception ex)
        {
            ProgressBorder.IsVisible = false;
            await ShowErrorDialog($"Conversion failed: {ex.Message}");
        }
        finally
        {
            ConvertButton.IsEnabled = true;
            // Stop loading animation if still running
            try
            {
                _loadingCts?.Cancel();
                _loadingCts = null;
                ProgressBar.IsIndeterminate = false;
            }
            catch { }
        }
    }

    private async Task CreateTestProcedures(List<string> inputPaths)
    {
        try
        {
            _convertedFilePaths.Clear();
            var results = new List<string>();

            for (int i = 0; i < inputPaths.Count; i++)
            {
                var inputPath = inputPaths[i];
                
                // Generate output path
                var directory = Path.GetDirectoryName(inputPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var fileName = Path.GetFileNameWithoutExtension(inputPath);
                var outputPath = Path.Combine(directory, $"{fileName}_converted.pdf");
                _convertedFilePaths.Add(outputPath);

                // Update progress bar and face animation
                ProgressBar.Value = i;








                // Call test procedures creation function here






                // Below code may not be needed:

                // Perform conversion on background thread to avoid blocking UI
                await Task.Run(() =>
                {
                    // Load the Word document
                    var doc = new Document(inputPath);

                    // Save as PDF
                    doc.Save(outputPath, SaveFormat.Pdf);
                });

                results.Add($"✓ {Path.GetFileName(inputPath)} → {Path.GetFileName(outputPath)}");
            }

            // Complete animation and update UI
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = ProgressBar.Maximum = inputPaths.Count;
            
            ConvertingText.Text = "Complete!";
            ProgressBar.IsVisible = false;
            ResultBorder.IsVisible = true;
            DownloadPanel.IsVisible = true;
            
            ResultText.Text = $"Summary of conversion:\n\n{string.Join("\n", results)}";
        }
        catch (Exception ex)
        {
            ProgressBorder.IsVisible = false;
            throw new Exception($"Failed to parse documents: {ex.Message}");
        }
    }

    private void ResetUIState()
    {
        try
        {
            // Hide all result sections
            ProgressBorder.IsVisible = false;
            ResultBorder.IsVisible = false;
            DownloadPanel.IsVisible = false;
            
            // Reset progress bar
            ProgressBar.Value = 0;
            ProgressBar.IsVisible = true;

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error resetting UI state: {ex.Message}");
        }
    }

    private async void DownloadButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_convertedFilePaths.Count == 0 || _convertedFilePaths.Any(path => !File.Exists(path)))
        {
            await ShowErrorDialog("No converted files available for download.");
            return;
        }

        try
        {
            if (_convertedFilePaths.Count == 1)
            {
                // Single file - use save dialog
                var options = new FilePickerSaveOptions
                {
                    Title = "Save PDF As",
                    SuggestedFileName = Path.GetFileName(_convertedFilePaths[0]),
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
                    File.Copy(_convertedFilePaths[0], file.Path.LocalPath, overwrite: true);
                    await ShowInfoDialog($"PDF saved successfully to: {file.Path.LocalPath}");
                }
            }
            else
            {
                // Multiple files - open folder
                OpenFolderButton_Click(sender, e);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialog($"Error saving file: {ex.Message}");
        }
    }

    private async void OpenFolderButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_convertedFilePaths.Count == 0 || _convertedFilePaths.Any(path => !File.Exists(path)))
        {
            await ShowErrorDialog("No converted files available.");
            return;
        }

        try
        {
            var folderPath = Path.GetDirectoryName(_convertedFilePaths[0]);
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

    private async Task ShowDialog(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
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

    private async Task ShowErrorDialog(string message) => await ShowDialog("Error", message);
    private async Task ShowInfoDialog(string message) => await ShowDialog("Success", message);

    private async void AeronixLink_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            // Open the Aeronix website in the default browser
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.aeronix.com/",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening Aeronix website: {ex.Message}");
            // Show user-friendly error message
            await ShowErrorDialog($"Unable to open Aeronix website. Please visit https://www.aeronix.com/ manually.");
        }
    }
}