using System;
using System.IO;
using CommandLine;
using Belmondo.WolfMapLib;

class Program
{
    static void Main(string[] args)
    {
        var mapSet = new MacMapSet(new FileInfo("D:/.rsrc/ Second Encounter (30 Levels)"));
        mapSet.GetMap(0);
    }
}
