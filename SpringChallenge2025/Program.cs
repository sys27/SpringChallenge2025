using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SpringChallenge2025;

public sealed class Memory
{
    public static readonly Memory Instance = new Memory();

    private readonly Board[] boards = new Board[11];
    private int index = 0;

    public void Add(Board board)
    {
        Debug.Assert(index < boards.Length);

        boards[index] = board;
        index++;
    }

    public void Clear()
    {
        for (var i = 0; i < boards.Length; i++)
            boards[i] = default;

        index = 0;
    }

    public Board[] Boards => boards;

    public int Count => index;
}

public readonly struct Board
{
    private readonly int depth;
    private readonly uint board;

    public Board(int depth, uint board)
    {
        this.depth = depth;
        this.board = board;
    }

    public uint GetHash()
    {
        var hash = 0u;
        const uint mask = 0b111;

        for (var i = 0; i < 9; i++)
        {
            var value = (board >> (i * 3)) & mask;
            if (value == 0)
                continue;

            hash += value * (uint)Math.Pow(10, i);
        }

        return hash;
    }

    public override string ToString()
        => GetHash().ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint GetByIndex(int index)
        => index < 0
            ? 0
            : (board >> (index * 3)) & 0b111u;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint GetMask(int index)
        => 0b111u << (index * 3);

    private static ReadOnlySpan<byte> Positions =>
    [
        0b1111,

        0b1110,
        0b1101,
        0b1011,
        0b0111,

        0b0011,
        0b0101,
        0b0110,
        0b1001,
        0b1010,
        0b1100,
    ];

    private static readonly uint[] values = new uint[4];
    private static readonly uint[] masks = new uint[4];

    public Memory GetNextBoards()
    {
        for (var cell = 0; cell < 9; cell++)
        {
            var value = GetByIndex(cell);
            if (value != 0)
                continue;

            var up = GetByIndex(cell + 3);
            values[0] = cell + 3 < 9 && up != 0 ? up : 6;

            var down = GetByIndex(cell - 3);
            values[1] = cell - 3 >= 0 && down != 0 ? down : 6;

            var left = GetByIndex(cell + 1);
            values[2] = (cell + 1) / 3 == cell / 3 && left != 0 ? left : 6;

            var right = GetByIndex(cell - 1);
            values[3] = (cell - 1) / 3 == cell / 3 && right != 0 ? right : 6;

            masks[0] = ~GetMask(cell + 3);
            masks[1] = ~GetMask(cell - 3);
            masks[2] = ~GetMask(cell + 1);
            masks[3] = ~GetMask(cell - 1);

            var merged = false;
            foreach (var position in Positions)
            {
                var current = 0u;
                for (var i = 0; i < values.Length; i++)
                    if ((position & (1 << i)) != 0)
                        current += values[i];

                if (current > 6)
                    continue;

                merged = true;

                var mergedBoard = board;
                for (var i = 0; i < masks.Length; i++)
                    if ((position & (1 << i)) != 0)
                        mergedBoard &= masks[i];

                mergedBoard |= current << (cell * 3);
                Memory.Instance.Add(new Board(depth + 1, mergedBoard));
            }

            if (!merged)
                Memory.Instance.Add(new Board(depth + 1, board | (1u << (cell * 3))));
        }

        return Memory.Instance;
    }

    public int Depth => depth;
}

public class Program
{
    private static IEnumerable<(int id, Board board, int maxDepth, uint final)> GetTestCase()
    {
        // #1
        // 20
        // 0 6 0
        // 2 2 2
        // 1 6 1
        // 322444322
        // 2
        // 1
        yield return (1, new Board(0, 0b000_110_000_010_010_010_001_110_001), 20, 322444322);

        // #2
        // 20
        // 5 0 6
        // 4 5 0
        // 0 6 4
        // 951223336
        // 6
        // 1
        yield return (2, new Board(0, 0b101_000_110_100_101_000_000_110_100), 20, 951223336);

        // #3
        // 1
        // 5 5 5
        // 0 0 5
        // 5 5 5
        // 36379286
        // 2
        // 2
        yield return (3, new Board(0, 0b101_101_101_000_000_101_101_101_101), 1, 36379286);

        // #4
        // 1
        // 6 1 6
        // 1 0 1
        // 6 1 6
        // 264239762
        // 11
        // 11
        yield return (4, new Board(0, 0b110_001_110_001_000_001_110_001_110), 1, 264239762);

        // #5
        // 8
        // 6 0 6
        // 0 0 0
        // 6 1 5
        // 76092874
        // 1484
        // 20
        yield return (5, new Board(0, 0b110_000_110_000_000_000_110_001_101), 8, 76092874);

        // #6
        // 24
        // 3 0 0
        // 3 6 2
        // 1 0 2
        // 661168294
        // 418440394
        // 241
        yield return (6, new Board(0, 0b011_000_000_011_110_010_001_000_010), 24, 661168294);

        // #7
        // 36
        // 6 0 4
        // 2 0 2
        // 4 0 0
        // 350917228
        // 1014562252076
        // 2168
        yield return (7, new Board(0, 0b110_000_100_010_000_010_100_000_000), 36, 350917228);

        // #8
        // 32
        // 0 0 0
        // 0 5 4
        // 1 0 5
        // 999653138
        // 104530503002231
        // 4154
        yield return (8, new Board(0, 0b000_000_000_000_101_100_001_000_101), 32, 999653138);

        // #9
        // 40
        // 0 0 4
        // 0 2 4
        // 1 3 4
        // 521112022
        // 946763082877
        // 4956
        yield return (9, new Board(0, 0b000_000_100_000_010_100_001_011_100), 40, 521112022);

        // #10
        // 40
        // 0 5 4
        // 0 3 0
        // 0 3 0
        // 667094338
        // 559238314648167
        // 6044
        yield return (10, new Board(0, 0b000_101_100_000_011_000_000_011_000), 40, 667094338);

        // #11
        // 20
        // 0 5 1
        // 0 0 0
        // 4 0 1
        // 738691369
        // 4017226136890
        // 93190
        yield return (11, new Board(0, 0b000_101_001_000_000_000_100_000_001), 20, 738691369);

        // #12
        // 20
        // 1 0 0
        // 3 5 2
        // 1 0 0
        // 808014757
        // 950995003182
        // 94596
        yield return (12, new Board(0, 0b001_000_000_011_101_010_001_000_000), 20, 808014757);
    }

    public static void Main(string[] args)
    {
        const uint mod = 1u << 30;

        foreach (var (id, testCase, maxDepth, final) in GetTestCase())
        {
            var sw = Stopwatch.StartNew();

            var finalSum = 0u;
            var queue = new Queue<Board>();
            queue.Enqueue(testCase);

            while (queue.Count > 0)
            {
                var board = queue.Dequeue();
                if (board.Depth >= maxDepth)
                {
                    finalSum = (finalSum + board.GetHash()) % mod;
                    continue;
                }

                var added = false;
                var memory = board.GetNextBoards();
                for (var i = 0; i < memory.Count; i++)
                {
                    var b = memory.Boards[i];

                    added = true;
                    queue.Enqueue(b);
                }

                memory.Clear();

                if (!added)
                {
                    finalSum = (finalSum + board.GetHash()) % mod;
                }
            }

            sw.Stop();
            Console.WriteLine($"---------- #{id} ----------");
            Console.WriteLine($"Final sum: {finalSum}. Is valid: {finalSum == final}");
            Console.WriteLine($"Time: {sw.ElapsedMilliseconds} ms");
        }
    }

    // public static void Main(string[] args)
    // {
    //     var maxDepth = int.Parse(Console.ReadLine()!);
    //     var inputBoard = 0u;
    //     for (var i = 2; i >= 0; i--)
    //     {
    //         var input = Console.ReadLine()!.Split(' ');
    //         for (var j = 2; j >= 0; j--)
    //         {
    //             var value = uint.Parse(input[j]);
    //             if (value == 0)
    //                 continue;
    //
    //             inputBoard |= value << (i * 3 + j) * 3;
    //         }
    //     }
    //
    //     const uint mod = 1u << 30;
    //     var finalSum = 0u;
    //     var queue = new Queue<Board>();
    //     queue.Enqueue(new Board(0, inputBoard));
    //
    //     while (queue.Count > 0)
    //     {
    //         var board = queue.Dequeue();
    //         if (board.Depth >= maxDepth)
    //         {
    //             finalSum = (finalSum + board.GetHash()) % mod;
    //             continue;
    //         }
    //
    //         var added = false;
    //         var memory = board.GetNextBoards();
    //         for (var i = 0; i < memory.Count; i++)
    //         {
    //             var b = memory.Boards[i];
    //
    //             added = true;
    //             queue.Enqueue(b);
    //         }
    //
    //         memory.Clear();
    //
    //         if (!added)
    //         {
    //             finalSum = (finalSum + board.GetHash()) % mod;
    //         }
    //     }
    //
    //     Console.WriteLine(finalSum);
    // }
}