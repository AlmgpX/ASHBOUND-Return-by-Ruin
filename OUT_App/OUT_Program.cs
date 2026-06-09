using System;

namespace OUT_ASHBOUND;

public static class OUT_Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        OUT_Content content = OUT_Content.Load("OUT_Content");
        OUT_State state = OUT_Bootstrap.NewGame(content);
        OUT_RaylibApp app = new(state);
        app.Run();
    }
}
