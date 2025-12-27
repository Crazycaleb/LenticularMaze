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

    public Transform You;
    public GameObject[] Goals;

    public KMSelectable[] Arrows;

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

    int yourPosition = -1;
    int[] goalPositions = new int[] { -1, -1, -1 };
    bool[] discovered = new bool[] { false, false, false };
    string[] coords = "A1,B1,C1,D1,E1,F1,A2,B2,C2,D2,E2,F2,A3,B3,C3,D3,E3,F3,A4,B4,C4,D4,E4,F4,A5,B5,C5,D5,E5,F5,A6,B6,C6,D6,E6,F6".Split(',');
    string[] dirs = "up,right,down,left".Split(',');

    private void Awake()
    {
        ModuleId = ModuleIdCounter++;

        for (int a = 0; a < 4; a++)
        {
            int ax = a; //this is so incredibly dumb
            Arrows[a].OnInteract += delegate { ArrowPress(ax); return false; };
        }
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

        yourPosition = Rnd.Range(0, 36);
        reroll:
        for (int g = 0; g < 3; g++)
        {
            goalPositions[g] = Rnd.Range(0, 36);
            if (goalPositions[g] == yourPosition) { goto reroll; }
        }
        if (goalPositions[0] == goalPositions[1] || goalPositions[0] == goalPositions[2] || goalPositions[1] == goalPositions[2]) { goto reroll; }

        Debug.LogFormat("[Lenticular Maze #{0} You are at {1}, you need to make it to {2}, {3}, and {4}.", ModuleId, coords[yourPosition], coords[goalPositions[0]], coords[goalPositions[1]], coords[goalPositions[2]]);

        PlaceObjectHere(You, yourPosition);
        for (int g = 0; g < 3; g++)
        {
            PlaceObjectHere(Goals[g].transform, goalPositions[g]);
        }
    }

    void ArrowPress(int d)
    {
        if (ModuleSolved) { return; }

        if (_grid[yourPosition].Connections[d])
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Arrows[d].transform);
            switch (d)
            {
                case 0: yourPosition -= 6; break;
                case 1: yourPosition++; break;
                case 2: yourPosition += 6; break;
                case 3: yourPosition--; break;
            }
            PlaceObjectHere(You, yourPosition);

            if (goalPositions.Contains(yourPosition))
            {
                int goalIx = Array.IndexOf(goalPositions, yourPosition);
                if (!discovered[goalIx])
                {
                    discovered[goalIx] = true;
                    Goals[goalIx].SetActive(false);
                    Audio.PlaySoundAtTransform("collect", transform);
                    Debug.LogFormat("[Lenticular Maze #{0} You made it to the goal at {1}.", ModuleId, coords[goalPositions[goalIx]]);
                }
                if (!discovered.Contains(false))
                {
                    Module.HandlePass();
                    ModuleSolved = true;
                    Debug.LogFormat("[Lenticular Maze #{0} Made it to all three goals, module solved.", ModuleId);
                }
            }
        }
        else
        {
            Module.HandleStrike();
            Debug.LogFormat("[Lenticular Maze #{0} Can't move {1} from {2}. Strike!", ModuleId, dirs[d], coords[yourPosition]);
        }
    }

    void PlaceObjectHere(Transform go, int p)
    {
        int px = p % 6;
        int py = p / 6;
        go.localPosition = new Vector3(-0.06f + 0.02f * px, 0.0167f, 0.04f - 0.02f * py);
    }

    private void ApplyWallMaterials()
    {
        int hwpr = _size;
        int vwpr = _size - 1;

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size - 1; x++)
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
    private readonly string TwitchHelpMessage = @"!{0} move URDL [Move up, right, down, or left.]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(?:(move|press|submit)\s+)?(?<moves>[urdl;, ]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;

        var moves = m.Groups["moves"].Value;
        var list = new List<int>();
        for (int i = 0; i < moves.Length; i++)
        {
            int ix = "urdl;, ".IndexOf(moves[i]);
            if (ix == -1)
                yield break;
            if (ix > 3)
                continue;
            list.Add(ix);
        }
        yield return null;
        for (int i = 0; i <= list.Count; i++)
        {
            Arrows[i].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    struct QueueItem
    {
        public int Position;
        public int Parent;
        public int Direction;

        public QueueItem(int pos, int parent, int dir)
        {
            Position = pos;
            Parent = parent;
            Direction = dir;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        List<int> paths = new List<int>();
        var goals = goalPositions.Where((i, ix) => !discovered[ix]).ToList();
        int gc = goals.Count;
        int curPos = yourPosition;
        for (int i = 0; i < gc; i++)
        {
            var visited = new Dictionary<int, QueueItem>();
            var q = new Queue<QueueItem>();
            int goal = -1;
            q.Enqueue(new QueueItem(curPos, -1, 0));
            while (q.Count > 0)
            {
                var qi = q.Dequeue();
                if (visited.ContainsKey(qi.Position))
                    continue;
                visited[qi.Position] = qi;

                if (goals.Contains(qi.Position))
                {
                    goal = qi.Position;
                    break;
                }

                for (int dir = 0; dir < 4; dir++)
                    if (_grid[qi.Position].Connections[dir])
                        q.Enqueue(new QueueItem(GetNumInDir(qi.Position, dir), qi.Position, dir));
            }

            var r = goal;
            var path = new List<int>();
            while (true)
            {
                var nr = visited[r];
                if (nr.Parent == -1)
                    break;
                path.Add(nr.Direction);
                r = nr.Parent;
            }

            path.Reverse();
            paths.AddRange(path);
            curPos = goal;
            goals.Remove(goal);
        }
        for (int i = 0; i < paths.Count; i++)
        {
            Arrows[paths[i]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        yield break;
    }

    private int GetNumInDir(int pos, int dir)
    {
        switch (dir)
        {
            case 0: return pos - 6;
            case 1: return pos + 1;
            case 2: return pos + 6;
            case 3: return pos - 1;
        }
        throw new InvalidOperationException("Invalid dir");
    }
}
