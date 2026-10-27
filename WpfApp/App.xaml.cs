using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using XLib;
using XLib.UserControls;

namespace WpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
		MediaPlayer? player;
        static bool createdNew;
        Mutex mutex = new Mutex(true, "mutexofxplayer", out createdNew);
        const string pipeName = "pipeofxplayer";

        // \HKEY_LOCAL_MACHINE\SOFTWARE\Classes\SystemFileAssociations\.mp3\Shell\AddToXPlayerList
        // (Default) Add to XPlayer list
        // \HKEY_LOCAL_MACHINE\SOFTWARE\Classes\SystemFileAssociations\.mp3\Shell\AddToXPlayerList\Command
        // (Default) "J:\Programing\C#\VisualStudio2022\4.App\XPlayer\WpfApp\bin\Debug\net7.0-windows\XPlayer.exe" --add-to-playlist "%1"

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (createdNew)
            {
                InitializeXPlayer(e.Args);
            }
            else
            {
                if (e.Args.Length == 1)
                {
                    WriteToPipe(Cmd.Play + "|" + e.Args[0]);
                }
                else if (e.Args.Length == 2 && e.Args[0] == Cmd.Play)
                {
                    WriteToPipe(Cmd.Play + "|" + e.Args[1]);
                }
                else if (e.Args.Length == 2 && e.Args[0] == Cmd.AddToPlaylist)
                {
                    WriteToPipe(Cmd.AddToPlaylist + "|" + e.Args[1]);
                }

                Shutdown();
            }
        }

        void WriteToPipe(string msg)
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            pipe.Connect(3000);
            using var writer = new StreamWriter(pipe);
            writer.WriteLine(msg);
            writer.Flush();
        }

        void InitializeXPlayer(string[] args)
        {
            player = new();
            Current.MainWindow = player;
            player.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Images/Icon.ico", UriKind.RelativeOrAbsolute));
            player.ShouldClose = true;
            player.Show();

            if (args.Length == 1)
            {
                player.Play(args[0]);
            }
            else if (args.Length == 2 && args[0] == Cmd.Play)
            {
                player.Play(args[1]);
            }
            else if (args.Length == 2 && args[0] == Cmd.AddToPlaylist)
            {
                player.Play(args[1]);
            }

            Task.Run(() =>
            {
                while (true)
                {
                    using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In);
                    pipe.WaitForConnection();
                    using var reader = new StreamReader(pipe);
                    var msg = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(msg)) continue;
                    var cmd = msg.Split('|')[0];
                    var arg = msg.Split('|')[1];

                    if (cmd == Cmd.Play)
                    {
                        player.Dispatcher.Invoke(() => player.Play(arg));
                    }
                    else if (cmd == Cmd.AddToPlaylist)
                    {
                        player.Dispatcher.Invoke(() => player.AddToPlaylist(new string[] { arg }, false));
                    }
                }
            });
        }
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string message = $"{e.Exception.Message}\n{e.Exception.InnerException}\n{e.Exception.Source}\n{e.Exception.StackTrace}";
            message.ToFileText();
            DialogBox.ShowError(e.Exception.Message);
            e.Handled = true;
			
			player?.Stop();
        }
    }

    public static class Cmd
    {
        public const string Play = "--play";
        public const string AddToPlaylist = "--add-to-playlist";
    }
}
