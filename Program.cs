using ScreenRecorderLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SMEDKRecorder
{
    class Program
    {
        private static bool _isRecording;
        private static Stopwatch _stopWatch;
        private static List<RecordableWindow> windows;
        private static Model model;
        private static string filename = null;
        private static string _ip;
        private static int _port;
        private static string message = null;
        private static string path = "experiments/logs/voice/";
        static void Main(string[] args)
        {
            windows = Recorder.GetWindows();

            var opts = new RecorderOptions
            {
                AudioOptions = new AudioOptions
                {
                    IsAudioEnabled = false,
                },

                DisplayOptions = new DisplayOptions
                {
                    WindowHandle = windows[0].Handle
                },

                RecorderApi = RecorderApi.WindowsGraphicsCapture
            };

            Recorder rec = Recorder.CreateRecorder(opts);
            rec.OnRecordingFailed += Rec_OnRecordingFailed;
            rec.OnRecordingComplete += Rec_OnRecordingComplete;
            rec.OnStatusChanged += Rec_OnStatusChanged;
            Console.WriteLine("Press ENTER to start recording or ESC to exit");
            while (true)
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                if (info.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (info.Key == ConsoleKey.Escape)
                {
                    return;
                }
            }
            rec.Record(Path.ChangeExtension(Path.GetTempFileName(), ".mp4"));
            CancellationTokenSource cts = new CancellationTokenSource();
            var token = cts.Token;
            Task.Run(async () =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                        return;
                    if (_isRecording)
                    {
                        Console.Write(String.Format("\rElapsed: {0}s:{1}ms", _stopWatch.Elapsed.Seconds, _stopWatch.Elapsed.Milliseconds));
                    }
                    await Task.Delay(10);
                }
            }, token);
            while (true)
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                if (info.Key == ConsoleKey.Escape)
                {
                    break;
                }
            }
            cts.Cancel();
            rec.Stop();
            Console.WriteLine();

            Console.ReadKey();
        }

        private static void Rec_OnStatusChanged(object sender, RecordingStatusEventArgs e)
        {
            switch (e.Status)
            {
                case RecorderStatus.Idle:
                    //Console.WriteLine("Recorder is idle");
                    break;
                case RecorderStatus.Recording:
                    _stopWatch = new Stopwatch();
                    _stopWatch.Start();
                    _isRecording = true;
                    Console.WriteLine("Recording started");
                    Console.WriteLine("Press ESC to stop recording");
                    break;
                case RecorderStatus.Paused:
                    Console.WriteLine("Recording paused");
                    break;
                case RecorderStatus.Finishing:
                    Console.WriteLine("Finishing encoding");
                    break;
                default:
                    break;
            }
        }

        private static void Rec_OnRecordingComplete(object sender, RecordingCompleteEventArgs e)
        {
            Console.WriteLine("Recording completed");
            _isRecording = false;
            _stopWatch?.Stop();
            Console.WriteLine(String.Format("File: {0}", e.FilePath));
            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
        }

        private static void Rec_OnRecordingFailed(object sender, RecordingFailedEventArgs e)
        {
            Console.WriteLine("Recording failed with: " + e.Error);
            _isRecording = false;
            _stopWatch?.Stop();
            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
        }

        public static void FtpSend(string name)
        {
            string ftpfullpath = "ftp://" + model.ftp_host + "/" + path + name + ".mp3";
            FtpWebRequest ftp = (FtpWebRequest)FtpWebRequest.Create(ftpfullpath);
            ftp.Credentials = new NetworkCredential(model.ftp_login, model.ftp_password);
            ftp.KeepAlive = true;
            ftp.UseBinary = true;
            ftp.Proxy = null;
            ftp.Method = WebRequestMethods.Ftp.UploadFile;

            FileStream fs = File.OpenRead(name + ".mp3");
            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            fs.Close();
            Stream ftpstream = ftp.GetRequestStream();
            try
            {
                if (ftpstream.CanWrite)
                {
                    ftpstream.Write(buffer, 0, buffer.Length);
                    ftpstream.Close();
                    Console.WriteLine(string.Format("File: [{0}.mp3] передан по FTP", name));
                    File.Delete(name + ".mp3");
                }
                else
                {
                    Console.WriteLine("Ftp сервер запретил передачу файла");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Ошибка записи по FTP:{0} ", e.Message));
            }
        }
    }
}