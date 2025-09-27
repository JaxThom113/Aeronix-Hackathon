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

namespace MyApp;

public partial class MainWindow : Window
{
    private List<string> _selectedFilePaths = new List<string>();
    private List<string> _convertedFilePaths = new List<string>();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void DisplaySelectedFiles()
    {
        try
        {
            FileListPanel.Children.Clear();
            
            if (_selectedFilePaths?.Count == 0)
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
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
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
            await ShowErrorDialog("Please select valid Word documents first.");
            return;
        }

        try
        {
            // Show progress
            ProgressBorder.IsVisible = true;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Maximum = _selectedFilePaths.Count;
            ProgressBar.Value = 0;
            ResultBorder.IsVisible = false;
            DownloadPanel.IsVisible = false;
            ConvertButton.IsEnabled = false;

            // Perform conversion for all files
            await ConvertMultipleWordToPdfAsync(_selectedFilePaths);
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

    private async Task ConvertMultipleWordToPdfAsync(List<string> inputPaths)
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
                await UpdateFaceAnimation(i, inputPaths.Count);

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
            ProgressBar.Value = inputPaths.Count;
            await UpdateFaceAnimation(inputPaths.Count, inputPaths.Count);
            
            // Recenter mouth in the middle of the circle after rotation
            await RecenterMouthInCircle();
            
            ConvertingText.Text = "Complete!";
            ProgressBar.IsVisible = false;
            ResultBorder.IsVisible = true;
            DownloadPanel.IsVisible = true;
            
            ResultText.Text = $"Conversion Complete!\n\n{string.Join("\n", results)}";
        }
        catch (Exception ex)
        {
            ProgressBorder.IsVisible = false;
            throw new Exception($"Failed to convert documents: {ex.Message}");
        }
    }

    private async Task UpdateFaceAnimation(int current, int total)
    {
        try
        {
            // Calculate rotation angle based on progress
            var rotationAngle = total > 0 ? (current * 180.0) / total : 0.0;
            
            // Apply rotation around mouth center (15, 8) for 30px wide mouth
            MouthPath.RenderTransform = new RotateTransform
            {
                Angle = rotationAngle,
                CenterX = 15, // Center of 30px wide mouth
                CenterY = 8   // Center of mouth height
            };

            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating face animation: {ex.Message}");
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
            
            // Reset face to frown
            ResetFaceToFrown();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error resetting UI state: {ex.Message}");
        }
    }

    private void ResetFaceToFrown()
    {
        try
        {
            // Reset to original position
            MouthPath.RenderTransform = new RotateTransform
            {
                Angle = 0,
                CenterX = 15,
                CenterY = 8
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error resetting face: {ex.Message}");
        }
    }

    private async Task RecenterMouthInCircle()
    {
        try
        {
            // After rotation around (15, 8), recenter mouth in the middle of the 60px circle
            // Circle center is at (30, 30) relative to the face circle
            // Mouth needs to be moved to center of the circle
            var transform = new TransformGroup();
            
            // Keep the 180-degree rotation around (15, 8)
            transform.Children.Add(new RotateTransform
            {
                Angle = 180,
                CenterX = 15,
                CenterY = 8
            });
            
            // Add translation to center the mouth in the circle
            transform.Children.Add(new TranslateTransform
            {
                X = -25,  // Move 25px left to center horizontally
                Y = -10   // Move 10px up to center vertically
            });
            
            MouthPath.RenderTransform = transform;
            await Task.Delay(200);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error recentering mouth: {ex.Message}");
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