using System;
using UnknownGame.Save;
using UnityEngine;

namespace UnknownGame.GameFlow
{
    public static class GameSession
    {
        public static SaveGameData Current { get; private set; }

        public static void Begin(SaveGameData data)
        {
            if (data == null || !data.IsValid(out _))
                throw new ArgumentException("La partida de la sesión no es válida.", nameof(data));

            Current = data;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => Current = null;
    }
}
