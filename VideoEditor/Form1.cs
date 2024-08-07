using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Speech.Synthesis;
//using AForge.Video.FFMPEG;
using NAudio.Wave;
using System.Windows.Forms;
using System.IO;
using Accord.Video.FFMPEG;
using Accord.Audio;
using Accord.Audio.Formats;
using Accord.Video;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace VideoEditor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            SetRoundedButton(play, 20);
            SetRoundedButton(Start, 20);
            SetRoundedButton(Merging, 20);

            SetBorderlessButton(play);
            SetBorderlessButton(Start);
            SetBorderlessButton(Merging);

            
        }

        public void ConvertVideoToImages(string videoPath, string outputFolder)
        {
            Form1 obj = new Form1();
            using (VideoFileReader videoReader = new VideoFileReader())
            {
                // Open the video file
                videoReader.Open(videoPath);

                // Create output folder if it doesn't exist
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }
                

                // Loop through each frame in the video

                int chunkDurationInSeconds;
                if (!int.TryParse(part.Text, out chunkDurationInSeconds) || chunkDurationInSeconds <= 0)
                {
                    MessageBox.Show("Invalid chunk length. Please enter a positive integer.");
                    return;
                }

                int frameRate = 30;
                
                int framesPerChunk = frameRate * chunkDurationInSeconds;

                double totalFrames = videoReader.FrameCount;

                for (int i = 0; i < totalFrames; i += framesPerChunk)
                {
                    // Read frames for the next 5 seconds
                    for (int j = 0; j < framesPerChunk && (i + j) < totalFrames; j++)
                    {
                        Bitmap videoFrame = videoReader.ReadVideoFrame();

                        // Save the frame as an image
                        string imagePath = Path.Combine(outputFolder, $"chunk_{i / frameRate:D5}", $"frame_{i + j:D5}.png");
                        Directory.CreateDirectory(Path.GetDirectoryName(imagePath)); // Ensure the directory exists
                        videoFrame.Save(imagePath, ImageFormat.Png);

                        // Release the frame
                        videoFrame.Dispose();
                    }
                }

                // Close the video file
                videoReader.Close();
            }
        }

        public static void CreateVideo(string audioFilePath, string imageFolder, string outputVideoPath)
        {
            // Create an audio reader with the specified WAV file
            using (WaveFileReader audioReader = new WaveFileReader(audioFilePath))
            {
                // Desired video duration in seconds
                double desiredDuration = 10.0;

                // Calculate the total number of frames based on the frame rate
                int totalFrames = (int)(desiredDuration * 30); // Assuming 30 frames per second

                // Get a list of image files in the specified folder
                string[] imageFiles = Directory.GetFiles(imageFolder, "*.png");

                // Adjust the number of frames to the minimum between totalFrames and imageFiles.Length
                int framesToWrite = Math.Min(totalFrames, imageFiles.Length);

                using (VideoFileWriter writer = new VideoFileWriter())
                {
                    try
                    {
                        writer.Open(outputVideoPath, 1920, 1080, 30, VideoCodec.MPEG4);

                        // Set the video duration based on the number of frames
                        double videoDuration = framesToWrite / 30.0; // Assuming 30 frames per second

                        // Write each image frame to the video
                        for (int i = 0; i < framesToWrite; i++)
                        {
                            Bitmap frame = new Bitmap(imageFiles[i]);
                            writer.WriteVideoFrame(frame);

                            // Release the frame
                            frame.Dispose();
                        }

                        // Convert audio duration to the number of frames
                        int audioFrameCount = (int)(audioReader.TotalTime.TotalSeconds * audioReader.WaveFormat.SampleRate);

                        // Write the audio samples to the video using the hypothetical WriteAudioSamples method
                        byte[] audioBuffer = new byte[audioFrameCount * audioReader.WaveFormat.BlockAlign];
                        int bytesRead = audioReader.Read(audioBuffer, 0, audioBuffer.Length);
                        //writer.WriteAudioSamples(audioBuffer, 0, bytesRead);

                        Console.WriteLine($"Video created at: {outputVideoPath}");
                    }
                    finally
                    {
                        // Make sure to close the writer to release resources
                        writer.Close();
                    }
                }
            }
        }

        // Text to audio converter
        public static void GenerateWavFromText(string text, string outputFilePath)
        {
            using (SpeechSynthesizer synth = new SpeechSynthesizer())
            {
                // Set the voice and rate (optional)
                synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
                synth.Rate = 0; // Adjust the rate as needed

                // Set the output format to WAV
                synth.SetOutputToWaveFile(outputFilePath);

                // Convert text to speech and save to the specified file
                synth.Speak(text);
            }

            Console.WriteLine($"WAV file generated at: {outputFilePath}");
        }

        // Function to add subtitle to an image
        private static void AddSubtitleToImage(string inputImagePath, string outputImagePath, string subtitleText)
        {
            using (Bitmap originalImage = new Bitmap(inputImagePath))
            {
                using (Graphics graphics = Graphics.FromImage(originalImage))
                {
                    Font subtitleFont = new Font("Arial", 12, FontStyle.Regular);
                    SolidBrush subtitleBrush = new SolidBrush(Color.White);
                    PointF subtitlePosition = new PointF(10, originalImage.Height - 30);

                    graphics.DrawString(subtitleText, subtitleFont, subtitleBrush, subtitlePosition);
                    originalImage.Save(outputImagePath, ImageFormat.Png);
                }
            }
        }

        public static void MergeVideoWithAudio(string videoFilePath, string audioFilePath, string outputFilePath)
        {
            // Specify the path to the FFmpeg executable
            string ffmpegPath = @"C:\ffmpeg\bin\ffmpeg.exe"; // Replace with the actual path

            // Use FFmpeg to merge audio and video
            Process ffmpegProcess = new Process
            {
                StartInfo =
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{videoFilePath}\" -i \"{audioFilePath}\" -c:v copy -c:a aac -strict experimental -map 0:v:0 -map 1:a:0 \"{outputFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
                EnableRaisingEvents = true
            };

            ffmpegProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"FFmpeg Error: {e.Data}");
                }
            };

            ffmpegProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"FFmpeg Output: {e.Data}");
                }
            };

            ffmpegProcess.Start();
            ffmpegProcess.BeginErrorReadLine();
            ffmpegProcess.BeginOutputReadLine();
            ffmpegProcess.WaitForExit();
        }


        private void Start_Click(object sender, EventArgs e)
        {
            
            
            try
            {
                string videoPath = "D:\\Editor\\Sample.mp4";
                string imageOutputFolder = "D:\\Editor\\ImageOuput";
                string audioOutputFolder = "D:\\Editor\\AudioOutput";
                string finalVideoPath = "D:\\Editor\\FinalVideo\\final_video.mp4";
                string subtitleText = "This is Subtitle Text";
                MessageBox.Show("Video generation Start.");
                ConvertVideoToImages(videoPath, imageOutputFolder);

                MessageBox.Show("Image generation Complete.");

                GenerateWavFromText(subtitleText, $"{audioOutputFolder}/subtitle_audio.wav");

                MessageBox.Show("Subtitle generation Complete.");
                foreach (string imagePath in Directory.GetFiles(imageOutputFolder, "*.png"))
                {
                    AddSubtitleToImage(imagePath, imagePath, subtitleText);
                }
                MessageBox.Show("Video generation Start.");

                CreateVideo($"{audioOutputFolder}/subtitle_audio.wav", imageOutputFolder, finalVideoPath);

                MessageBox.Show("Video generation complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Start_Click: {ex.Message}");
            }
        }

        private void Merging_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.ShowDialog();
            file.Filter = "MP4 Files | *.mp4";
            string videoFilePath = file.FileName;

            OpenFileDialog Afile = new OpenFileDialog();
            Afile.ShowDialog();
            Afile.Filter = "MP3 Files | *.mp3";
            string audioFilePath = Afile.FileName;
            
            if(videoFilePath == "" ||  audioFilePath == "")
            {
                MessageBox.Show("Audio or video file is missing");
            }
            else
            {
                string outputFilePath = @"D:\Editor\video1.mp4";

                MergeVideoWithAudio(videoFilePath, audioFilePath, outputFilePath);

                MessageBox.Show("Audio and video merged successfully.");
            }

            
        }

        private void Chunk_TextChanged(object sender, EventArgs e)
        {

        }

        private void play_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.ShowDialog();
            string path = file.FileName;
            axWindowsMediaPlayer1.URL = path;
        }

        private void siticoneGradientButton1_Click(object sender, EventArgs e)
        {

        }

        private void SetRoundedButton(Button button, int borderRadius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, borderRadius, borderRadius, 180, 90);
            path.AddArc(button.Width - borderRadius, 0, borderRadius, borderRadius, 270, 90);
            path.AddArc(button.Width - borderRadius, button.Height - borderRadius, borderRadius, borderRadius, 0, 90);
            path.AddArc(0, button.Height - borderRadius, borderRadius, borderRadius, 90, 90);
            path.CloseFigure();
            button.Region = new Region(path);

            button.Paint += (sender, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            };
        }

        private void SetBorderlessButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            //button.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(0, 255, 255, 255); // Transparent border color
            
        }
    }
}
