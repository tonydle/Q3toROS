using UnityEngine;
using RosImage = RosMessageTypes.Sensor.ImageMsg;

namespace Unity.Robotics
{
    public class RosSubscriberImage : RosSubscriber<RosImage>
    {
        private Texture2D _texture2D;
        private RosImage _msg;
        private bool _ready = false;

        protected override void Start()
        {
            base.Start();
            _texture2D = new Texture2D(1, 1, TextureFormat.R8, false);
        }

        protected override void Update()
        {
            base.Update();
            if(NewMessageAvailable())
            {
                _ready = false;
                _msg = GetLatestMessage();
                if (_texture2D.width != _msg.width || _texture2D.height != _msg.height)
                {
                    _texture2D.Reinitialize((int)_msg.width, (int)_msg.height);
                }
                _texture2D.LoadRawTextureData(_msg.data);
                _texture2D.Apply();
                _ready = true;
            }
        }

        public bool isReady()
        {
            return _ready;
        }

        public Texture2D GetLatestTexture2D()
        {
            return _texture2D;
        }

        protected void OnDestroy()
        {
            if (_texture2D != null)
            {
                Destroy(_texture2D);
            }
        }
    }
}
