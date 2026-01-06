using System.Collections.Generic;
using RabbitVsMole.InteractableGameObject.Base;

namespace RabbitVsMole.Online
{
    /// <summary>
    /// Centralized authority switch for online play. Host is authoritative over
    /// field state transitions; clients can only apply state changes that were
    /// explicitly authorized by host.
    /// </summary>
    public static class OnlineAuthority
    {
        private static bool _isOnline;
        private static bool _isHost;
        private static readonly HashSet<FieldBase> _remotelyAuthorizedFields = new();
        private static System.Action<FieldBase, FieldState> _fieldStateSender;

        public static bool IsOnline => _isOnline;
        public static bool IsHost => _isHost;

        public static void Configure(bool isHost)
        {
            _isOnline = true;
            _isHost = isHost;
            _remotelyAuthorizedFields.Clear();
        }

        public static void Disable()
        {
            _isOnline = false;
            _isHost = false;
            _remotelyAuthorizedFields.Clear();
            _fieldStateSender = null;
        }

        public static void RegisterFieldStateSender(System.Action<FieldBase, FieldState> sender) =>
            _fieldStateSender = sender;

        /// <summary>
        /// Allows a single state change for a specific field when running as client.
        /// Host should call this before commanding the client to update the field.
        /// </summary>
        public static void AuthorizeRemoteFieldChange(FieldBase field)
        {
            if (!_isOnline || _isHost || field == null)
                return;

            _remotelyAuthorizedFields.Add(field);
        }

        public static bool CanChangeFieldState(FieldBase field)
        {
            if (!_isOnline)
                return true;

            if (_isHost)
                return true;

            // client side: allow only if previously authorized for this field
            if (field != null && _remotelyAuthorizedFields.Remove(field))
                return true;

            return false;
        }

        public static void NotifyHostFieldStateChanged(FieldBase field, FieldState newState)
        {
            if (!_isOnline || !_isHost)
                return;

            _fieldStateSender?.Invoke(field, newState);
        }
    }
}

