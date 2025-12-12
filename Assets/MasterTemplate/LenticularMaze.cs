using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class LenticularMaze : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    public GameObject[] HorizontalWalls;
    public GameObject[] VerticalWalls;
    public Material[] WallMaterials;

    public class SquareInfo
    {
        public int X;
        public int Y;
        public bool[] Connections = new bool[4];
    }

    sealed class DirectionInfo
    {
        public int Direction;
        public int DestIx;
    }

    private SquareInfo[] _grid;
    private const int _size = 6;

    private void Awake()
    {
        ModuleId = ModuleIdCounter++;
    }

    private void Start()
    {
        var todo = new List<SquareInfo>();
        var processed = new List<SquareInfo>();
        var done = new List<SquareInfo>();

        for (var i = 0; i < _size * _size; i++)
            done.Add(new SquareInfo { X = i % _size, Y = i / _size });

        var startX = Rnd.Range(0, _size);
        var startY = Rnd.Range(0, _size);

        var dx = new[] { 0, 1, 0, -1 };
        var dy = new[] { -1, 0, 1, 0 };

        for (var directionality = 0; directionality < 2; directionality++)
        {
            todo.AddRange(done);
            done.Clear();

            var startIx = todo.IndexOf(sq => sq.X == startX && sq.Y == startY);
            processed.Add(todo[startIx]);
            todo.RemoveAt(startIx);

            while (todo.Count > 0)
            {
                var pIx = Rnd.Range(0, processed.Count);
                var x = processed[pIx].X;
                var y = processed[pIx].Y;

                var availableConnections = new List<DirectionInfo>();
                int destIx;
                for (var dir = 0; dir < 4; dir++)
                    if ((destIx = todo.IndexOf(sq => sq.X == x + dx[dir] && sq.Y == y + dy[dir])) != -1)
                        availableConnections.Add(new DirectionInfo { Direction = dir, DestIx = destIx });

                if (availableConnections.Count == 0)
                {
                    done.Add(processed[pIx]);
                    processed.RemoveAt(pIx);
                }
                else
                {
                    var cn = availableConnections[Rnd.Range(0, availableConnections.Count)];
                    if (directionality == 0)
                        processed[pIx].Connections[cn.Direction] = true;
                    else
                        todo[cn.DestIx].Connections[(cn.Direction + 2) % 4] = true;
                    processed.Add(todo[cn.DestIx]);
                    todo.RemoveAt(cn.DestIx);
                }
            }

            done.AddRange(processed);
            processed.Clear();
        }

        _grid = done.OrderBy(cell => cell.Y * _size + cell.X).ToArray();

        for (int i = 0; i < _grid.Length; i++)
        {
            Debug.LogFormat("[Lenticular Maze #{0}] {1},{2}, Connections: {3}", ModuleId, _grid[i].X, _grid[i].Y, _grid[i].Connections.Select(b => b ? "1" : "0").Join(""));
        }

        ApplyWallMaterials();
    }


    private void ApplyWallMaterials()
    {
        int hwpr = _size;
        int vwpr = _size - 1;

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size -1; x++)
            {
                int a = y * _size + x;
                int b = a + 1;

                bool ab = _grid[a].Connections[1];
                bool ba = _grid[b].Connections[3];

                int matIx = (ab ? 2 : 0) | (ba ? 1 : 0);

                int wIx = y * vwpr + x;
                VerticalWalls[wIx].GetComponent<MeshRenderer>().material = WallMaterials[matIx];
            }
        }

        for (int y = 0; y < _size - 1; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                int a = y * _size + x;
                int b = a + _size;

                bool ab = _grid[a].Connections[2];
                bool ba = _grid[b].Connections[0];

                int matIx = (ab ? 2 : 0) | (ba ? 1 : 0);

                int wIx = y * hwpr + x;
                HorizontalWalls[wIx].GetComponent<MeshRenderer>().material = WallMaterials[matIx];
            }
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
}
