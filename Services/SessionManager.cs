using System;
using System.Xml.Linq;
using System.Collections.Concurrent;

namespace ZitaDataSystem.Services
{
    public class SessionManager
    { private readonly ConcurrentDictionary<string, XDocument> _sessions = new ConcurrentDictionary<string, XDocument>();
        // Starts a session with the given XML document and returns a session ID.
    public string StartSession(XDocument data)
    {
        string sessionId = Guid.NewGuid().ToString();
        Console.WriteLine($"Here is the data\n {data}");
        _sessions[sessionId] = data;
        Console.WriteLine($"[DEBUG] Starting session {sessionId} with XML: {data}");
        return sessionId;
    }

    // Retrieves a session without removing it.
    public XDocument? GetSession(string sessionHash)
    {
        if (_sessions.TryGetValue(sessionHash, out var data))
        {
            Console.WriteLine($"[DEBUG] Found session for '{sessionHash}': {data}");
            return data;
        }
        Console.WriteLine($"[DEBUG] Session '{sessionHash}' not found.");
        return null;
    }

    // EndSession: Retrieves and removes the session.
    public XDocument? EndSession(string sessionHash)
    {
        if (_sessions.TryRemove(sessionHash, out var data))
        {
            Console.WriteLine($"[DEBUG] Removed session for '{sessionHash}': {data}");
            return data;
        }
        Console.WriteLine($"[DEBUG] Session {sessionHash} not found.");
        return null;
    }

    // Stores or updates a session.
    public void StoreSession(string sessionHash, XDocument sessionData)
    {
        Console.WriteLine($"[DEBUG] Storing session: {sessionHash} -> {sessionData}");
        _sessions[sessionHash] = sessionData;
    }
}
}
