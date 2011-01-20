using Mono.Terminal;

class Test {
	static void Main ()
	{
		Application.Init (false);
		var d = new Container (0, 0, Application.Cols, Application.Lines);

		d.Add (new Label (10, 10, "Text"));
		d.Add (new Entry (16, 10, 20, "Edit me"));
		Application.Run (d);
	}
}
