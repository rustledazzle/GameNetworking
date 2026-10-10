using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single frame of player input, tagged with a sequence number and timestamp.
/// Sent from client to server for authoritative processing.
/// </summary>
[Serializable]
public struct InputCommand
{
    public uint sequenceNumber;     // unique per frame, incremented client-side
    public float timestamp;         // Time.time when input was captured
    public Vector2 moveInput;       // x = horizontal (A/D), y = vertical (W/S)
    public bool jump;               // Space
    public bool sprint;             // Left Shift
}

/// <summary>
/// A rolling buffer of unacknowledged input commands.
/// The client replays these when the server sends a correction.
/// </summary>
public class InputBuffer
{
    private readonly Queue<InputCommand> buffer = new Queue<InputCommand>();

    public int Count => buffer.Count;

    public void Push(InputCommand cmd)
    {
        buffer.Enqueue(cmd);
    }

    /// <summary>
    /// Removes all commands up to and including the given sequence number.
    /// Called after the server acknowledges processing them.
    /// </summary>
    public void DropOlderThan(uint seq)
    {
        while (buffer.Count > 0 && buffer.Peek().sequenceNumber <= seq)
            buffer.Dequeue();
    }

    /// <summary>
    /// Returns all remaining (unacknowledged) commands as an array.
    /// Used during reconciliation to replay them from the server's authoritative position.
    /// </summary>
    public InputCommand[] GetAll()
    {
        return buffer.ToArray();
    }

    public void Clear()
    {
        buffer.Clear();
    }
}