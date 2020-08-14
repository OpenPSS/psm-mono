using System;
using NUnitLite.Runner;

class Runner
{
	[STAThread]
	public static void Main(string[] args)
	{
		new TextUI().Execute(args);
	}
}
