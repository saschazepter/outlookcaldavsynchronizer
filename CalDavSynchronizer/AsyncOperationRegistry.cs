using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CalDavSynchronizer
{
  public interface ICancellationTokenFactory
  {
    CancellationToken CreateCancellationToken(string operationName);
  }

  public class AsyncOperationRegistry : ICancellationTokenFactory
  {
    public CancellationToken CreateCancellationToken(string operationName)
    {
      return CancellationToken.None;
    }

  }

  public class NullCancellationTokenFactory : ICancellationTokenFactory
  {
    public static readonly ICancellationTokenFactory Instance = new NullCancellationTokenFactory();

    private NullCancellationTokenFactory()
    {
    }

    public CancellationToken CreateCancellationToken(string operationName)
    {
      return CancellationToken.None;
    }
  }
}
