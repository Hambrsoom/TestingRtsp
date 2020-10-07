using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

    unsafe class Program
    {

        static void Main(string[] args)
        {
            int err;
            int _streamIndex = -1;
            AVCodecContext* _avCodecContext;

            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_ERROR);
            ffmpeg.RootPath = Environment.CurrentDirectory;
            ffmpeg.avformat_network_init();

            AVFormatContext* _avFormatContext = ffmpeg.avformat_alloc_context();

            if (_avFormatContext == null)
                throw new InvalidOperationException($"Cannot allocate AvFormat context at {nameof(Program)}");

            err = ffmpeg.avformat_open_input(&_avFormatContext, "rtsp://localhost:8554/vlc", null, null);
            if (err < 0)
            {
                throw new InvalidOperationException($"Cannot open input stream. Error code: {err}");
            }


            err = ffmpeg.avformat_find_stream_info(_avFormatContext, null);
            if (err < 0)
            {
                throw new InvalidOperationException($"Cannot find stream info");
            }

            AVStream* stream = null;
            for (var i = 0; i < _avFormatContext->nb_streams; i++)
            {
                if (_avFormatContext->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                {
                    stream = _avFormatContext->streams[i];
                    _streamIndex = i;
                }
            }

            if (stream == null || _streamIndex == -1)
            {
                throw new InvalidOperationException("Cannot get video stream from context");
            }

            _avCodecContext = stream->codec;

            ffmpeg.av_read_play(_avFormatContext);


        var codec = ffmpeg.avcodec_find_decoder(_avCodecContext->codec_id);
        if (codec == null)
        {
            throw new InvalidOperationException("Invalid codec");
        }

        err = ffmpeg.avcodec_open2(_avCodecContext, codec, null);

        if (err < 0)
        {
            throw new InvalidOperationException("Cannot open codec");
        }

        Console.WriteLine(ffmpeg.avcodec_get_name(_avCodecContext->codec_id));

        int _width = _avCodecContext->width;
        int _height = _avCodecContext->height;
        var _pixFmt = _avCodecContext->pix_fmt;

        Console.WriteLine($"Resolution: {_avCodecContext->width}x{_avCodecContext->height}");
        Console.WriteLine($"Pixel Format: {_avCodecContext->pix_fmt}");

        SwsContext* _swsContext = ffmpeg.sws_getContext(_width, _height, _pixFmt, _width, _height, AVPixelFormat.AV_PIX_FMT_BGR24, ffmpeg.SWS_FAST_BILINEAR, null, null, null);

        if (_swsContext == null)
        {
            throw new InvalidOperationException($"Cannot create an SwsContext.");
        }

        IntPtr _swsBufferPtr = Marshal.AllocHGlobal(ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_BGR24, _width, _height, 1));

        if (_swsBufferPtr == null)
        {
            throw new InvalidOperationException($"Could not allocate pointer to frame memory.");
        }

        byte_ptrArray4 _swsFrameData = new byte_ptrArray4();
        int_array4 _swsFrameLineSize = new int_array4();

        err = ffmpeg.av_image_fill_arrays(ref _swsFrameData, ref _swsFrameLineSize, (byte*)_swsBufferPtr, AVPixelFormat.AV_PIX_FMT_BGR24, _width, _height, 1);
        if (err < 0)
        {
            throw new InvalidOperationException("Cannot fill arrays");
        }
    }
    }
