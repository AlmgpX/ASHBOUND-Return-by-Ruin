using System;

namespace OUT_ASHBOUND;

public static class OUT_Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;
        try
        {
            OUT_Content content = OUT_Content.Load("OUT_Content");
            OUT_State state = OUT_Bootstrap.NewGame(content);
            OUT_App app = new(state);
            app.Run();
        }
        finally
        {
            Console.ResetColor();
            Console.CursorVisible = true;
        }
    }
}
