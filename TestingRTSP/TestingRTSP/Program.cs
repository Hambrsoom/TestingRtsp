using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

unsafe class Program
{

    static void decode(AVCodecContext* dec_ctx, AVFrame* frame, AVPacket* pkt)
    {
        int ret;

        // Send packet to decoder
        ret = ffmpeg.avcodec_send_packet(dec_ctx, pkt);
        if (ret < 0)
        {
            throw new InvalidOperationException("Error sending packet for decoding.");
            Environment.Exit(0);
        }
        while (ret >= 0)
        {
            // receive frame from decoder
            // we may receive multiple frames or we may consume all data from decoder, then return to main loop
            ret = ffmpeg.avcodec_receive_frame(dec_ctx, frame);
            if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                return;
            else if (ret < 0)
            {
                // something wrong, quit program
                throw new InvalidOperationException("Error during decoding.");
                Environment.Exit(0);
            }
            Console.WriteLine("Frame: " + dec_ctx->frame_number);
        }
    }

    static void Main(string[] args)
    {
        int err;
        int _streamIndex = -1;
        AVCodecContext* _avCodecContext;
        AVPacket* _avPacket;
        AVFrame* _avFrame;

        ffmpeg.av_log_set_level(ffmpeg.AV_LOG_ERROR);
        ffmpeg.RootPath = Environment.CurrentDirectory;
        ffmpeg.avformat_network_init();

        // Initialize context
        AVFormatContext* _avFormatContext = ffmpeg.avformat_alloc_context();

        if (_avFormatContext == null)
            throw new InvalidOperationException($"Cannot allocate AvFormat context at {nameof(Program)}");

        // Open input file
        err = ffmpeg.avformat_open_input(&_avFormatContext, "C:\\Users\\tlgmz\\Downloads\\project.mp4", null, null);
        if (err < 0)
        {
            throw new InvalidOperationException($"Cannot open input stream. Error code: {err}");
        }

        // Get stream info
        err = ffmpeg.avformat_find_stream_info(_avFormatContext, null);
        if (err < 0)
        {
            throw new InvalidOperationException($"Cannot find stream info");
        }

        // Get the video stream 
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

        // Initialize codec context
        _avCodecContext = stream->codec;

        // Start playing the stream
        //ffmpeg.av_read_play(_avFormatContext);

        // Find decoding codec
        var codec = ffmpeg.avcodec_find_decoder(_avCodecContext->codec_id);
        if (codec == null)
        {
            throw new InvalidOperationException("Invalid codec. Cannot find decoder.");
        }

        // Try to open codec
        err = ffmpeg.avcodec_open2(_avCodecContext, codec, null);
        if (err < 0)
        {
            throw new InvalidOperationException("Cannot open decoded.");
        }

        // Testing purposes
        Console.WriteLine(ffmpeg.avcodec_get_name(_avCodecContext->codec_id));

        int _width = _avCodecContext->width;
        int _height = _avCodecContext->height;
        var _pixFmt = _avCodecContext->pix_fmt;

        Console.WriteLine($"Resolution: {_avCodecContext->width}x{_avCodecContext->height}");
        Console.WriteLine($"Pixel Format: {_avCodecContext->pix_fmt}");

        // Initialize packet
        _avPacket = ffmpeg.av_packet_alloc();
        ffmpeg.av_init_packet(_avPacket);
        if (_avPacket == null)
        {
            throw new InvalidOperationException("Cannot initialize packet.");
        }

        // Initialize frame
        _avFrame = ffmpeg.av_frame_alloc();
        if (_avPacket == null)
        {
            throw new InvalidOperationException("Cannot initialize frame.");
        }

        // main loop
        int ret;
        while (1 == 1)
        {
            // Read an econded packet from file
            ret = ffmpeg.av_read_frame(_avFormatContext, _avPacket);
            if (ret < 0)
            {
                ffmpeg.av_log(null, ffmpeg.AV_LOG_ERROR, "Cannot read frame.");
                break;
            }

            // If packet data is video data, then send it to decoder
            if (_avPacket->stream_index == _streamIndex)
            {
                decode(_avCodecContext, _avFrame, _avPacket);
            }

            // release packet buffers to be allocated again
            ffmpeg.av_packet_unref(_avPacket);
        }

        //// No clue
        //SwsContext* _swsContext = ffmpeg.sws_getContext(_width, _height, _pixFmt, _width, _height, AVPixelFormat.AV_PIX_FMT_BGR24, ffmpeg.SWS_FAST_BILINEAR, null, null, null);

        //if (_swsContext == null)
        //{
        //    throw new InvalidOperationException($"Cannot create an SwsContext.");
        //}

        //IntPtr _swsBufferPtr = Marshal.AllocHGlobal(ffmpeg.av_image_get_buffer_size(AVPixelFormat.AV_PIX_FMT_BGR24, _width, _height, 1));

        //if (_swsBufferPtr == null)
        //{
        //    throw new InvalidOperationException($"Could not allocate pointer to frame memory.");
        //}

        //byte_ptrArray4 _swsFrameData = new byte_ptrArray4();
        //int_array4 _swsFrameLineSize = new int_array4();

        //err = ffmpeg.av_image_fill_arrays(ref _swsFrameData, ref _swsFrameLineSize, (byte*)_swsBufferPtr, AVPixelFormat.AV_PIX_FMT_BGR24, _width, _height, 1);
        //if (err < 0)
        //{
        //    throw new InvalidOperationException("Cannot fill arrays");
        //}
        //}
    }
}
