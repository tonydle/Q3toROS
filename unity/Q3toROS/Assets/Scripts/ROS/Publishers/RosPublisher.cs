using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

namespace Unity.Robotics
{
    public abstract class RosPublisher<T> : MonoBehaviour where T : ROSTCPConnector.MessageGeneration.Message
    {
        [SerializeField] protected string m_topic = "";
        protected virtual void Start()
        {
            _ = ROSConnection.GetOrCreateInstance().RegisterPublisher<T>(m_topic);
        }

        protected void Publish(T message)
        {
            if (message != null)
            {
                ROSConnection.GetOrCreateInstance().Publish(m_topic, message);
            }
        }

        public void SetTopic(string topic)
        {
            m_topic = topic;
        }
    }
}