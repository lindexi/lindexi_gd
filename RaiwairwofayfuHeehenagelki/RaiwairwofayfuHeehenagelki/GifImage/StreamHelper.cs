using System;
using System.Collections.Generic;
using System.IO;

namespace RaiwairwofayfuHeehenagelki.GifImage
{
    internal class GifStream : Stream
    {
        internal GifStream(Stream stream)
        {
            _stream = stream;
        }

        private readonly Stream _stream;

        //读取指定长度的字节字节
        internal byte[] ReadByte(int len)
        {
            var buffer = new byte[len];
            _stream.Read(buffer, 0, len);
            return buffer;
        }

        /// <summary>
        ///     读取一个字节
        /// </summary>
        /// <returns></returns>
        internal int Read()
        {
            return _stream.ReadByte();
        }

        private short ReadShort()
        {
            var buffer = new byte[2];
            _stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToInt16(buffer, 0);
        }

        internal string ReadString(int length)
        {
            return new string(ReadChar(length));
        }

        private char[] ReadChar(int length)
        {
            var buffer = new byte[length];
            _stream.Read(buffer, 0, length);
            var charBuffer = new char[length];
            buffer.CopyTo(charBuffer, 0);
            return charBuffer;
        }


        #region 从文件流中读取应用程序扩展块

        /// <summary>
        ///     从文件流中读取应用程序扩展块
        /// </summary>
        /// <returns></returns>
        internal ApplicationEx GetApplicationEx()
        {
            var appEx = new ApplicationEx();
            var blockSize = Read();
            if (blockSize != ApplicationEx.BlockSize)
            {
                throw new Exception("数据格式错误！");
            }

            appEx.ApplicationIdentifier = ReadChar(8);
            appEx.ApplicationAuthenticationCode = ReadChar(3);
            var nextFlag = Read();
            appEx.Data = new List<DataStruct>();
            while (nextFlag != 0)
            {
                var data = new DataStruct(nextFlag, this);
                appEx.Data.Add(data);
                nextFlag = Read();
            }

            return appEx;
        }

        #endregion

        #region 从文件数据流中读取注释扩展块

        internal CommentEx GetCommentEx()
        {
            var cmtEx = new CommentEx { CommentData = new List<string>() };
            var nextFlag = Read();
            cmtEx.CommentData = new List<string>();
            while (nextFlag != 0)
            {
                var blockSize = nextFlag;
                var data = ReadString(blockSize);
                cmtEx.CommentData.Add(data);
                nextFlag = Read();
            }

            return cmtEx;
        }

        #endregion

        #region 从文件数据流中读取注释扩展块

        /// <summary>
        ///     从文件数据流中读取图形文本扩展(Plain Text Extension)
        /// </summary>
        /// <returns></returns>
        internal PlainTextEx GetPlainTextEx()
        {
            var plainText = new PlainTextEx();
            var blockSize = Read();
            if (blockSize != PlainTextEx.BlockSize)
            {
                throw new Exception("数据格式错误！");
            }

            plainText.XOffSet = ReadShort();
            plainText.YOffSet = ReadShort();
            plainText.Width = ReadShort();
            plainText.Height = ReadShort();
            plainText.CharacterCellWidth = (byte) Read();
            plainText.CharacterCellHeight = (byte) Read();
            plainText.ForegroundColorIndex = (byte) Read();
            plainText.BgColorIndex = (byte) Read();
            var nextFlag = Read();
            plainText.TextDatas = new List<string>();
            while (nextFlag != 0)
            {
                blockSize = nextFlag;
                var data = ReadString(blockSize);
                plainText.TextDatas.Add(data);
                nextFlag = Read();
            }

            return plainText;
        }

        #endregion

        #region 从文件数据流中读取注释扩展块

        /// <summary>
        ///     从文件数据流中读取 图象标识符(Image Descriptor)
        /// </summary>
        /// <returns></returns>
        internal ImageDescriptor GetImageDescriptor()
        {
            var imageDescriptor = new ImageDescriptor
            {
                XOffSet = ReadShort(),
                YOffSet = ReadShort(),
                Width = ReadShort(),
                Height = ReadShort(),
                Packed = (byte) Read()
            };

            imageDescriptor.LocalColorTableFlag = (imageDescriptor.Packed & 0x80) >> 7 == 1;
            imageDescriptor.InterlaceFlag = (imageDescriptor.Packed & 0x40) >> 6 == 1;
            imageDescriptor.SortFlag = (imageDescriptor.Packed & 0x20) >> 5 == 1;
            imageDescriptor.LocalColorTableSize = 2 << (imageDescriptor.Packed & 0x07);
            return imageDescriptor;
        }

        #endregion

        #region 从文件数据流中读取图形控制扩展(Graphic Control Extension)

        /// <summary>
        ///     从文件数据流中读取图形控制扩展(Graphic Control Extension)
        /// </summary>
        /// <returns></returns>
        internal GraphicEx GetGraphicControlExtension()
        {
            var graphic = new GraphicEx();
            var blockSize = Read();
            if (blockSize != GraphicEx.BlockSize)
            {
                throw new Exception("数据格式错误！");
            }

            graphic.Packed = (byte) Read();
            graphic.TransparencyFlag = (graphic.Packed & 0x01) == 1;
            graphic.DisposalMethod = (graphic.Packed & 0x1C) >> 2;
            graphic.Delay = ReadShort();
            graphic.TranIndex = (byte) Read();
            Read();
            return graphic;
        }

        #endregion

        #region 从文件数据流中逻辑屏幕标识符(Logical Screen Descriptor)

        /// <summary>
        ///     从文件数据流中读取图形控制扩展(Graphic Control Extension)
        /// </summary>
        /// <returns></returns>
        internal LogicalScreenDescriptor GetLogicalScreenDescriptor()
        {
            var logicalScreenDescriptor = new LogicalScreenDescriptor();
            logicalScreenDescriptor.Width = ReadShort();
            logicalScreenDescriptor.Height = ReadShort();
            logicalScreenDescriptor.Packed = (byte) Read();
            logicalScreenDescriptor.GlobalColorTableFlag = (logicalScreenDescriptor.Packed & 0x80) >> 7 == 1;
            logicalScreenDescriptor.ColorResoluTion = (byte) ((logicalScreenDescriptor.Packed & 0x60) >> 5);
            logicalScreenDescriptor.SortFlag = (byte) (logicalScreenDescriptor.Packed & 0x10) >> 4;
            logicalScreenDescriptor.GlobalColorTableSize = 2 << (logicalScreenDescriptor.Packed & 0x07);
            logicalScreenDescriptor.BgColorIndex = (byte) Read();
            logicalScreenDescriptor.PixcelAspect = (byte) Read();
            return logicalScreenDescriptor;
        }

        #endregion

        /// <inheritdoc />
        public override void Flush()
        {
            _stream.Flush();
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            _stream.SetLength(value);
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        /// <inheritdoc />
        public override bool CanRead => _stream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => _stream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => _stream.CanWrite;

        /// <inheritdoc />
        public override long Length => _stream.Length;

        /// <inheritdoc />
        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }
    }
}