using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

namespace Unity.Robotics
{
    public abstract class RosPublisher<T> : MonoBehaviour where T: ROSTCPConnector.MessageGeneration.Message
    {
        [SerializeField] protected string _topic = "";
        protected T _message;
        protected virtual void Start()
        {
            ROSConnection.GetOrCreateInstance().RegisterPublisher<T>(_topic);
        }

        protected void Publish(T message)
        {                
            if(message != null)
            {
                ROSConnection.GetOrCreateInstance().Publish(_topic, message);
            }
        }

        public void SetTopic(string topic)
        {
            _topic = topic;
        }
    }
}