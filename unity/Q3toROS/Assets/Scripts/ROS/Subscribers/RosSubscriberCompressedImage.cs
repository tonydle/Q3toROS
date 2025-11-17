using UnityEngine;
using LightBuzz.Jpeg;
using RosCompressedImage = RosMessageTypes.Sensor.CompressedImageMsg;

namespace Unity.Robotics
{
    public class RosSubscriberCompressedImage : RosSubscriber<RosCompressedImage>
    {
        private Texture2D _texture2D;
        private RosCompressedImage _msg;
        private JpegDecoder jpegDecoder;
        private bool _ready = false;

        protected override void Start()
        {
            base.Start();
            _texture2D = new Texture2D(1, 1, TextureFormat.RGB24, false);
            jpegDecoder = new JpegDecoder();
        }

        protected override void Update()
        {
            base.Update();
            if(NewMessageAvailable())
            {
                _ready = false;
                _msg = GetLatestMessage();
                if (_msg.format.Contains("jpeg"))
                {
                    byte[] rawData = jpegDecoder.Decode(_msg.data, PixelFormat.RGB, Flag.NONE, out int width, out int height);
                    if (_texture2D.width != width || _texture2D.height != height)
                    {
                        _texture2D.Reinitialize(width, height);
                    }
                    _texture2D.LoadRawTextureData(rawData);
                    _texture2D.Apply();
                }
                else if(_msg.format.Contains("png"))
                {
                    ImageConversion.LoadImage(_texture2D, _msg.data);
                }
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
