using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using System.Collections.Generic;

namespace Unity.Robotics
{
    public abstract class RosSubscriber<T> : MonoBehaviour where T: ROSTCPConnector.MessageGeneration.Message
    {
        [SerializeField] protected string _topic = "";
        [SerializeField] protected int _queueSize = 1;
        protected Queue<T> _incomingMessages;
        T _latestMessage;
        protected bool _newMessageAvailable = false;

        protected virtual void Start()
        {
            // create a Queue of _queueSize initial capacity (which can increases)
            _incomingMessages = new Queue<T>(_queueSize);
            ROSConnection.GetOrCreateInstance().Subscribe<T>(_topic, ReceiveCallback);
        }

        protected virtual void Update()
        {
            // if no new messages
            if(_incomingMessages.Count == 0)
                return;

            //flush old messages
            while(_incomingMessages.Count > _queueSize)
            {
                _incomingMessages.Dequeue();
            }
        }

        public void SetTopic(string topic)
        {
            _topic = topic;
        }

        protected bool NewMessageAvailable()
        {
            return _newMessageAvailable;
        }

        protected void ReceiveCallback(T message)
        {                
            if(message != null)
            {
                _latestMessage = message;
                _incomingMessages.Enqueue(_latestMessage);
                _newMessageAvailable = true;
            }
        }

        protected T GetLatestMessage()
        {
            _newMessageAvailable = false;
            return _latestMessage;
        }

        protected Queue<T> GetMessageQueue()
        {
            _newMessageAvailable = false;
            return _incomingMessages;
        }
    }
}