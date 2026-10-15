using System.Collections.Generic;

namespace TDOM.Gameplay.Core
{
    public sealed class ConfirmedCharacterSelection
    {
        public enum Character
        {
            None = 0,
            Zendre = 1,
            Ayla = 2,
        }

        public struct Player
        {
            public ulong ClientId { get; }
            public Character SelectedCharacter { get; }
            public bool Ready { get; }

            internal Player(ulong clientId, Character selectedCharacter, bool ready)
            {
                ClientId = clientId;
                SelectedCharacter = selectedCharacter;
                Ready = ready;
            }
        }

        private readonly List<Player> _players = new List<Player>();

        public IReadOnlyList<Player> Players => _players;

        public bool Elegir(ulong clientId, Character character)
        {
            if (character != Character.Zendre && character != Character.Ayla)
                return false;

            for (int i = 0; i < _players.Count; i++)
                if (_players[i].ClientId != clientId && _players[i].SelectedCharacter == character)
                    return false;

            int index = Find(clientId);
            var player = new Player(clientId, character, false);
            if (index >= 0)
                _players[index] = player;
            else
                _players.Add(player);
            return true;
        }

        public void MarcarListo(ulong clientId)
        {
            int index = Find(clientId);
            if (index < 0 || _players[index].SelectedCharacter == Character.None)
                return;
            var player = _players[index];
            _players[index] = new Player(clientId, player.SelectedCharacter, true);
        }

        public bool PuedeIniciar
        {
            get
            {
                if (_players.Count != 2)
                    return false;
                foreach (var player in _players)
                    if (player.SelectedCharacter == Character.None || !player.Ready)
                        return false;
                return true;
            }
        }

        public void Desconectar(ulong clientId)
        {
            int index = Find(clientId);
            if (index < 0)
                return;
            _players.RemoveAt(index);
            for (int i = 0; i < _players.Count; i++)
            {
                var player = _players[i];
                _players[i] = new Player(player.ClientId, player.SelectedCharacter, false);
            }
        }

        private int Find(ulong clientId)
        {
            for (int i = 0; i < _players.Count; i++)
                if (_players[i].ClientId == clientId)
                    return i;
            return -1;
        }
    }
}
