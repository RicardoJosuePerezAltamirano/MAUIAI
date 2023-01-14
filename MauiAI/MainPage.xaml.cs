using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Media;
using System.Diagnostics;

namespace MauiAI;

public partial class MainPage : ContentPage
{
    private readonly IMediaPicker mediaPicker;
    int count = 0;
    private const int ImageMaxSizeBytes = 4194304;
    private const int ImageMaxResolution = 1024;

    public MainPage(IMediaPicker mediaPicker)
	{
		InitializeComponent();
        this.mediaPicker = mediaPicker;
    }

	private async void OnCounterClicked(object sender, EventArgs e)
	{
        if (mediaPicker.IsCaptureSupported)
        {
            FileResult photo = await mediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                var resizedPhoto = await ResizePhotoStream(photo);

                //string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                //using Stream source = await photo.OpenReadAsync();
                //using FileStream localFile = File.OpenWrite(localFilePath);
                //await source.CopyToAsync(localFile);

                // Custom Vision API call
                var result = await ClassifyImage(new MemoryStream(resizedPhoto));

                // Change the percentage notation from 0.9 to display 90.0%
                var percent = result.Probability.ToString("P1");

                Photo.Source = ImageSource.FromStream(() => new MemoryStream(resizedPhoto));

                Result.Text = result.TagName.Equals("Negative")
                  ? "No es un felino."
                  : $"es  {percent} un {result.TagName}.";
            
             }
        }
    }
    private async Task<byte[]> ResizePhotoStream(FileResult photo)
    {
        byte[] result = null;

        using (var stream = await photo.OpenReadAsync())
        {
            if (stream.Length > ImageMaxSizeBytes)
            {
                var image = PlatformImage.FromStream(stream);
                if (image != null)
                {
                    var newImage = image.Downsize(ImageMaxResolution, true);
                    result = newImage.AsBytes();
                }
            }
            else
            {
                using (var binaryReader = new BinaryReader(stream))
                {
                    result = binaryReader.ReadBytes((int)stream.Length);
                }
            }
        }

        return result;
    }

    private async Task<PredictionModel> ClassifyImage(Stream photoStream)
    {
        try
        {
            

            var endpoint = new CustomVisionPredictionClient(new ApiKeyServiceClientCredentials(ApiKeys.PredictionKey))
            {
                Endpoint = ApiKeys.CustomVisionEndPoint
            };

            // Send image to the Custom Vision API
            var results = await endpoint.ClassifyImageAsync(Guid.Parse(ApiKeys.ProjectId), ApiKeys.PublishedName, photoStream);

            // Return the most likely prediction
            return results.Predictions?.OrderByDescending(x => x.Probability).FirstOrDefault();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return new PredictionModel();
        }
        finally
        {
            
        }
    }
}


/*
 * 
 * How to use the Prediction API
If you have an image URL:
https://demoforxamarin-prediction.cognitiveservices.azure.com/customvision/v3.0/Prediction/e54d62d1-299f-4083-a5ea-6e25411da4d7/classify/iterations/Iteration1/url
Set Prediction-Key Header to : 74555927d37e4a95a06a213efa291c9c
Set Content-Type Header to : application/json
Set Body to : {"Url": "https://example.com/image.png"}
If you have an image file:
https://demoforxamarin-prediction.cognitiveservices.azure.com/customvision/v3.0/Prediction/e54d62d1-299f-4083-a5ea-6e25411da4d7/classify/iterations/Iteration1/image
Set Prediction-Key Header to : 74555927d37e4a95a06a213efa291c9c
Set Content-Type Header to : application/octet-stream
Set Body to : <image file>

 * */