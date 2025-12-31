using System;
using GL2Engine;

namespace GL2Engine;

public static class Program
{
  [STAThread]
  static void Main()
  {
    using (var game = new Game1())
      game.Run();
  }
}
