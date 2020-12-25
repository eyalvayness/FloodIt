using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FloodIt.Game;
using FloodIt.Models;
using FloodIt.Utils;

namespace FloodIt.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        readonly static int minSize = 6;
        readonly static int maxSize = 12;
        int _size, _moves;
        readonly UniformGrid _container;
        readonly Channel<Brush> _channel;
        Game.Game _game;
        CancellationTokenSource source;

        public int Size
        {
            get => _size;
            set
            {
                SetProperty(ref _size, value);
                CreateNewGame();
            }
        }
        public int Moves { get => _moves; set => SetProperty(ref _moves, value); }

        public Command ChangeSizeCommand { get; }
        public Command RestartCommand { get; }

        public MainWindowViewModel(UniformGrid container)
        {
            _container = container;
            _channel = Channel.CreateUnbounded<Brush>(new UnboundedChannelOptions());
            ChangeSizeCommand = new(ChangeSize, CanChangeSize);
            RestartCommand = new(Restart);

            Moves = 0;
            Size = (minSize + maxSize) / 2;
        }

        void Restart() => Size = _size;
        void CreateNewGame()
        {
            if (source != null)
            {
                source.Cancel(false);
                source.Dispose();
            }
            source = new();
            while (_channel.Reader.TryRead(out Brush b));
            _game = new Game.Game(_container, new CommandRectangleCreation(_channel.Writer), new GameSettings() { Size = Size });
            Moves = 0;
            _game.OnBrushPlayed += (e, b) => Moves++;
            //_game.StartGame(new UserStrategy(_channel.Reader), source.Token);
            Task.Run(async () =>
            {
                try
                {
                    bool won = await _game.StartGame(new UserStrategy(_channel.Reader), source.Token);
                    if (won)
                        System.Windows.MessageBox.Show("You won !", "GG", System.Windows.MessageBoxButton.OK);
                }
                catch (OperationCanceledException) { }
                catch (Exception) { }
            });
        }

        bool CanChangeSize(object i) => i is string val && minSize <= Size + int.Parse(val) && Size + int.Parse(val) <= maxSize;
        void ChangeSize(object i) => Size += int.Parse(i as string);
    }

    public class UserStrategy : IStrategy
    {
        readonly ChannelReader<Brush> _reader;
        
        public UserStrategy(ChannelReader<Brush> reader)
        {
            _reader = reader;
        }

        public async Task<Brush> Play(GameState state, CancellationToken cancellationToken)
        {
            await _reader.WaitToReadAsync(cancellationToken);
            Brush b = await _reader.ReadAsync(cancellationToken);
            
            return b;
        }
    }

    public class BasicRectangleCreation : IRectangleCreation
    {
        public virtual Rectangle GetNewRectangle(Brush[] allBrushes)
        {
            Brush brush = allBrushes.Random();
            var rect = new Rectangle() { Fill = brush };
            
            return rect;
        }
    }

    public class CommandRectangleCreation : BasicRectangleCreation
    {
        readonly ChannelWriter<Brush> _writer;

        public Command<Brush> Command { get; }

        public CommandRectangleCreation(ChannelWriter<Brush> writer)
        {
            _writer = writer;
            Command = new(Execute);
        }

        async void Execute(Brush b) => await _writer.WriteAsync(b);

        public override Rectangle GetNewRectangle(Brush[] allBrushes)
        {
            Rectangle rect = base.GetNewRectangle(allBrushes);
            rect.InputBindings.Add(new MouseBinding(Command, new MouseGesture(MouseAction.LeftClick)) { CommandParameter = rect.Fill });

            return rect;
        }
    }
}
