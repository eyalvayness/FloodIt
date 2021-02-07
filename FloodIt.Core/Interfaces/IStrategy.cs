using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FloodIt.Core.Interfaces
{
    public interface IStrategy
    {
        Brush Play(GameState state);
    }

    public interface IAsyncStrategy
    {
        Task<Brush> PlayAsync(GameState state, CancellationToken cancellationToken);
    }
}
