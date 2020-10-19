using Accord.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace ScreenRecorderDemo
{
    class ScreenRecorder
    {
        // Video variables
        private Rectangle _bounds;
        private string _outputPath;

        private string tempPath;
        private int fileCount = 1;
        private List<string> inputImageSequence = new List<string>();

        // File variables:
        private string audioName = "tempMic.wav";
        private string videoName = "video.mp4";
        private string finalName = $"{DateTime.Now:ddMMyyyy_HHm}-rec.mp4";

        // Time variable:
        Stopwatch watch = new Stopwatch();

        // Audio variables:
        public static class NativeMethods
        {
            [DllImport("winmm.dll", EntryPoint = "mciSendStringA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
            public static extern int record(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);
        }

        public ScreenRecorder(Rectangle bounds, string outputPath)
        {
            _bounds = bounds;
            _outputPath = outputPath;

            tempPath = $@"{Path.GetTempPath()}\MnemosyneTempRecordFiles";
        }

        public void CreateFolderIfNotExists()
        {
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
        }

        // In case of some unexpected exception clean up trash files from temp directory
        public void ExceptionCleanUp()
        {
            if (Directory.Exists(tempPath))
            {
                DeleteTempRecordingFiles(tempPath);
            }
        }

        public string GetElapsed()
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", watch.Elapsed.Hours, watch.Elapsed.Minutes, watch.Elapsed.Seconds);
        }

        public void RecordVideo()
        {
            watch.Start();

            try
            {
                using (var bitmap = new Bitmap(_bounds.Width, _bounds.Height))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(new System.Drawing.Point(_bounds.Left, _bounds.Top), System.Drawing.Point.Empty, _bounds.Size);
                    }

                    var name = $@"{tempPath}\screenshot-{fileCount}.png";

                    bitmap.Save(name, ImageFormat.Png);
                    inputImageSequence.Add(name);
                    fileCount++;
                    
                    bitmap.Dispose();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        public void Stop()
        {
            watch.Stop();

            var width = _bounds.Width;
            var height = _bounds.Height;
            var frameRate = 16;

            try
            {
                SaveAudio();
                SaveVideo(width, height, frameRate);
                CombineAudioWithVideo(videoName, audioName);
                DeleteTempRecordingFiles(tempPath);
                DeleteFilesExcept(_outputPath);
            }
            catch(Exception ex)
            {
                ExceptionCleanUp();
                MessageBox.Show(ex.Message, "Error");
            }
        }

        public void RecordAudio()
        {
            NativeMethods.record("open new Type waveaudio Alias recsound", string.Empty, 0, 0);
            NativeMethods.record("record recsound", string.Empty, 0, 0);
        }

        public void SetEnvPathForFfmpeg()
        {
            string startupPath = Environment.CurrentDirectory;
            var name = "PATH";
            var scope = EnvironmentVariableTarget.Machine;
            var oldValue = Environment.GetEnvironmentVariable(name, scope);

            if (!oldValue.Contains(startupPath))
            {
                var newValue = oldValue + $@";{startupPath}";
                Environment.SetEnvironmentVariable(name, newValue, scope);
            }
        }

        private void SaveVideo(int width, int height, int frameRate)
        {
            using (var vfWriter = new VideoFileWriter())
            {
                vfWriter.Open($@"{_outputPath}\{videoName}", width, height, frameRate, VideoCodec.MPEG4);

                foreach(var imageLocation in inputImageSequence)
                {
                    var imageFrame = System.Drawing.Image.FromFile(imageLocation) as Bitmap;
                    vfWriter.WriteVideoFrame(imageFrame);
                    imageFrame.Dispose();
                }

                vfWriter.Close();
                vfWriter.Dispose();
            }
        }

        private void SaveAudio()
        {
            var audioPath = $"save recsound {_outputPath}//{audioName}";

            NativeMethods.record(audioPath, string.Empty, 0, 0);
            NativeMethods.record("close recsound", string.Empty, 0, 0);
        }

        private void CombineAudioWithVideo(string video, string audio)
        {
            var command = $"/c ffmpeg -i \"{video}\" -i \"{audio}\" -shortest {finalName}"; // use /k insead of /c if you dont want to see cmd window

            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                FileName = "cmd.exe",
                WorkingDirectory = _outputPath,
                Arguments = command
            };

            using(var exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }

        private void DeleteTempRecordingFiles(string directory)
        {
            try
            {
                string[] files = Directory.GetFiles(directory);
                foreach (var file in files)
                {
                    // We need to set attribute as normal or we won't be able to modify and delete it
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
            finally
            {
                Directory.Delete(directory, false);
            }
        }

        private void DeleteFilesExcept(string targetFile)
        {
            string[] files = Directory.GetFiles(targetFile);

            foreach (var file in files.Where(file => !file.Contains("rec")))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
        }
    }
}
