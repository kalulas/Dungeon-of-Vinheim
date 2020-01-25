using System.Collections.Generic;
using UnityEngine;

namespace Invector
{
    [vClassHeader("MESSAGE RECEIVER", "Use this component with the vMessageSender to call Events.")]
    public class vMessageReceiver : vMonoBehaviour
    {
        public OnReceiveMessageEvent defaultListener;
        public List<vMessageListener> messagesListeners;

        [System.Serializable]
        public class OnReceiveMessageEvent : UnityEngine.Events.UnityEvent<string> { }

        [System.Serializable]
        public class vMessageListener
        {
            public string Name;
            public OnReceiveMessageEvent onReceiveMessage;
            public vMessageListener(string name)
            {
                this.Name = name;
            }
            public vMessageListener(string name, UnityEngine.Events.UnityAction<string> listener)
            {
                this.Name = name;
                this.onReceiveMessage.AddListener(listener);
            }
        }

        /// <summary>
        /// Add Action Listener
        /// </summary>
        /// <param name="name">Message Name</param>
        /// <param name="listener">Action Listener</param>
        public void AddListener(string name, UnityEngine.Events.UnityAction<string> listener)
        {
            if (messagesListeners.Exists(l => l.Name.Equals(name)))
            {
                var messageListener = messagesListeners.Find(l => l.Name.Equals(name));
                messageListener.onReceiveMessage.AddListener(listener);
            }
            else
            {
                messagesListeners.Add(new vMessageListener(name, listener));
            }
        }

        /// <summary>
        /// Remove Action Listener
        /// </summary>
        /// <param name="name">Message Name</param>
        /// <param name="listener">Action Listener</param>
        public void RemoveListener(string name, UnityEngine.Events.UnityAction<string> listener)
        {
            if (messagesListeners.Exists(l => l.Name.Equals(name)))
            {
                var messageListener = messagesListeners.Find(l => l.Name.Equals(name));
                messageListener.onReceiveMessage.RemoveListener(listener);
            }
        }

        /// <summary>
        /// Call Action with message
        /// </summary>
        /// <param name="name">message name</param>
        /// <param name="message">message value</param>
        public void Send(string name, string message)
        {
            if (messagesListeners.Exists(l => l.Name.Equals(name)))
            {
                var messageListener = messagesListeners.Find(l => l.Name.Equals(name));
                messageListener.onReceiveMessage.Invoke(message);
            }
            else defaultListener.Invoke(message);
        }

        /// <summary>
        /// Call Action without message
        /// </summary>
        /// <param name="name">message name</param>
        public void Send(string name)
        {
            if (messagesListeners.Exists(l => l.Name.Equals(name)))
            {
                var messageListener = messagesListeners.Find(l => l.Name.Equals(name));
                messageListener.onReceiveMessage.Invoke(string.Empty);
            }
            else defaultListener.Invoke(string.Empty);
        }
    }
}