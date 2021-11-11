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
using FloodIt.Models;
using FloodIt.Utils;
using FloodIt.Core;
using FloodIt.AI.NN;
using Microsoft.Win32;

namespace FloodIt.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        readonly static int minSize = 4;
        readonly static int maxSize = 12;
        int _size, _moves;
        readonly UniformGrid _container;
        readonly Channel<Brush> _channel;
        Game? _game;
        CancellationTokenSource? source;
        CompiledNeuralNetwork? _ai;
        string? _cnnName;

        public string? CnnName { get => _cnnName; set => SetProperty(ref _cnnName, value); }
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
        public Command OpenAICommand { get; }
        public Command AIPlayCommand { get; }
        public Command MCPlayCommand { get; }

        public MainWindowViewModel(UniformGrid container)
        {
            _container = container;
            _channel = Channel.CreateUnbounded<Brush>(new UnboundedChannelOptions());
            ChangeSizeCommand = new(ChangeSize, CanChangeSize);
            RestartCommand = new(Restart);
            OpenAICommand = new(OpenAI, CanOpenAI);
            AIPlayCommand = new(AIPlay, CanAIPlay);
            MCPlayCommand = new(MCPlay, CanMCPlay);

            Moves = 0;
            Size = 4;// (minSize + maxSize) / 2;
        }

        bool CanMCPlay() => true;
        async void MCPlay()
        {
            var mc = new AI.Algos.MonteCarlo();
            var settings = new GameSettings() { Size = Size };
            var rectCreation = new BasicRectangleCreation();

            if (source != null)
            {
                source.Cancel(false);
                source.Dispose();
            }
            source = new();

            _container.Children.Clear();
            for (int i = 0; i < settings.Count; i++)
                _container.Children.Add(rectCreation.GetNewRectangle());

            _game = new Game(GetBrush, SetBrush, settings);
            Moves = 0;
            _game.OnBrushPlayed += (e, b) => Moves++;

            try
            {
                await _game.StartGameAsync(mc, false, source.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        bool CanOpenAI() => true;
        void OpenAI()
        {
            var dialog = new OpenFileDialog()
            {
                Multiselect = false,
                Title = "Choose a serialized Compiled NN"
            };
            if (dialog.ShowDialog() == true)
            {
                CnnName = dialog.SafeFileName;
                _ai = CompiledNeuralNetwork.Load(dialog.FileName);
                if (_ai == null)
                {
                    System.Windows.MessageBox.Show($"Error while tryingh to open file {dialog.FileName}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
            }
        }

        bool CanAIPlay() => _ai != null;
        async void AIPlay()
        {
            if (source != null)
            {
                source.Cancel(false);
                source.Dispose();
            }
            source = new();

            var settings = new GameSettings() { Size = Size };
            var rectCreation = new BasicRectangleCreation();

            _container.Children.Clear();
            for (int i = 0; i < settings.Count; i++)
                _container.Children.Add(rectCreation.GetNewRectangle());

            _game = new Game(GetBrush, SetBrush, settings);
            Moves = 0;
            _game.OnBrushPlayed += (e, b) => Moves++;

            try
            {
                await _ai!.PlayAsync(_game, 1000, source.Token);
                //System.Windows.MessageBox.Show($"The AI finished in {Moves} move(s)! With {averageR:0.##} as average reward!", "GG", System.Windows.MessageBoxButton.OK);
                //App.Current.Dispatcher.Invoke(CreateNewGame);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        void Restart() => Size = _size;
        async void CreateNewGame()
        {
            if (source != null)
            {
                source.Cancel(false);
                source.Dispose();
            }
            source = new();
            while (_channel.Reader.TryRead(out Brush? _)) ;


            var settings = new GameSettings() { Size = Size };
            var rectCreation = new CommandRectangleCreation(_channel.Writer);

            _container.Children.Clear();
            for (int i = 0; i < settings.Count; i++)
                _container.Children.Add(rectCreation.GetNewRectangle());

            _game = new Game(GetBrush, SetBrush, settings);
            Moves = 0;
            _game.OnBrushPlayed += (e, b) => Moves++;

            //var ai = new QLearning(0.1, 0.99);
            //_game.StartGame(new UserStrategy(_channel.Reader), source.Token);
            //Task.Run(async () =>
            //{
            //    try
            //    {
            //        var averageR = await _ai.LearnAsync(_game);
            //        //bool won = await _game.StartGame(new UserStrategy(_channel.Reader), source.Token);
            //        //if (won)
            //        //    System.Windows.MessageBox.Show($"You won in {Moves} move(s)!", "GG", System.Windows.MessageBoxButton.OK);
            //        System.Windows.MessageBox.Show($"The AI finished in {Moves} move(s)! With {averageR:0.##} as average reward!", "GG", System.Windows.MessageBoxButton.OK);
            //        App.Current.Dispatcher.Invoke(CreateNewGame);
            //    }
            //    catch (OperationCanceledException) { }
            //    catch (Exception) { }
            //});        
            try
            {
                bool won = await _game.StartGameAsync(new UserStrategy(_channel.Reader), colorAsync: true, cancellationToken: source.Token);
                if (won)
                    System.Windows.MessageBox.Show($"You won in {Moves} move(s)!", "GG", System.Windows.MessageBoxButton.OK);
                //System.Windows.MessageBox.Show($"The AI finished in {Moves} move(s)! With {averageR:0.##} as average reward!", "GG", System.Windows.MessageBoxButton.OK);
                //App.Current.Dispatcher.Invoke(CreateNewGame);
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        bool CanChangeSize(object? i) => i is string val && minSize <= Size + int.Parse(val) && Size + int.Parse(val) <= maxSize;
        void ChangeSize(object? i) => Size += int.Parse(i?.ToString() ?? throw new NullReferenceException());


        Rectangle GetRectangleFromIndex(int index) => _container.Children[index] as Rectangle ?? throw new NullReferenceException();
        Brush GetBrush(int index)
        {
            if (_container.Dispatcher.Thread == Thread.CurrentThread)
                return GetRectangleFromIndex(index).Fill ?? throw new NullReferenceException();
            return _container.Dispatcher.Invoke(() => GetRectangleFromIndex(index).Fill);
        }

        void SetBrush(int index, Brush brush)
        {
            if (_container.Dispatcher.Thread == Thread.CurrentThread)
                GetRectangleFromIndex(index).Fill = brush;
            _container.Dispatcher.Invoke(() => GetRectangleFromIndex(index).Fill = brush);
        }
    }

    public class UserStrategy : Core.Interfaces.IAsyncStrategy
    {
        readonly ChannelReader<Brush> _reader;

        public UserStrategy(ChannelReader<Brush> reader)
        {
            _reader = reader;
        }

        public async Task<Brush?> PlayAsync(GameState state, CancellationToken cancellationToken)
        {
            await _reader.WaitToReadAsync(cancellationToken);
            Brush b = await _reader.ReadAsync(cancellationToken);

            return b;
        }
    }

    public class BasicRectangleCreation : FloodIt.Core.Interfaces.IRectangleCreation
    {
        public virtual Rectangle GetNewRectangle()
        {
            var rect = new Rectangle();

            return rect;
        }
    }

    public class CommandRectangleCreation : BasicRectangleCreation
    {
        readonly ChannelWriter<Brush> _writer;
        public Command<Rectangle> Command { get; }

        public CommandRectangleCreation(ChannelWriter<Brush> writer)
        {
            _writer = writer;
            Command = new(Execute);
        }

        async void Execute(Rectangle? r)
        {
            _ = r ?? throw new NullReferenceException();
            await _writer.WriteAsync(r.Fill);
        }

        public override Rectangle GetNewRectangle()
        {
            var rect = base.GetNewRectangle();
            rect.InputBindings.Add(new MouseBinding() { Command = Command, Gesture = new MouseGesture(MouseAction.LeftClick), CommandParameter = rect });

            return rect;
        }
    }
}
