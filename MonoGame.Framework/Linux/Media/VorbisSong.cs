#region License
/*
Microsoft Public License (Ms-PL)
MonoGame - Copyright © 2013 The MonoGame Team

All rights reserved.

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under 
U.S. copyright law.

A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
purpose and non-infringement.
*/
#endregion License

using System;
using System.IO;
using System.Collections.Generic;
#if MONOMAC
using MonoMac.OpenAL;
#else
using OpenTK.Audio.OpenAL;
#endif

namespace Microsoft.Xna.Framework.Media
{
    internal sealed class VorbisSong
    {
        private IntPtr vorbisStruct;

        private string fileName;

        private STBVorbis.Info vorbisInfo;

        private uint sizeInSamples;

	private bool isFinished;

        internal VorbisSong (string fileName)
        {           
            this.fileName = fileName;

            int error = 0;

            vorbisStruct = STBVorbis.OpenFilename (fileName, out error, IntPtr.Zero);

            if (error != 0) {
                Console.WriteLine (String.Format ("stb_vorbis error: {0}", error));
            } 

            vorbisInfo = STBVorbis.GetInfo(vorbisStruct);

            sizeInSamples = STBVorbis.StreamLengthInSamples(vorbisStruct);

	    isFinished = false;

        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (vorbisStruct != IntPtr.Zero)
                {
                    STBVorbis.Close(vorbisStruct);
                }
            }
        }

        public bool IsSongFinished()
	{
		return isFinished || (STBVorbis.GetSampleOffset(vorbisStruct) >= sizeInSamples);
	}
        
        public TimeSpan Duration
        {
            get {
                return new TimeSpan(1000 * sizeInSamples / vorbisInfo.sample_rate);
            }
        }

        public int SampleRate {
            get {
                return (int)vorbisInfo.sample_rate;
            }
        }

        public int BufferSize {
            get {
                return 4096;
            }
        }

        #region The Vorbis Decode Function
        // This is basically copied from the vorbis streaming in VideoPlayer.cs
        // However, that uses libvorbis/TheoraPlay and we use stb_vorbis. We also
        // are using 16 bit ints, not floats, for ease of use in the API.

        internal void FillBuffer(int buffer)
        {
            // Our buffer. temp_data is used to get around the lack of
            // efficient subarrays in C#. It takes one copy, which isn't _too_ bad.
            short[] data = new short[BufferSize];
            short[] temp_data = new short[BufferSize];

            int readData = 0;

            // Add to the buffer from the decoder until it's large enough.
            while (readData < BufferSize) {
                int readCall = STBVorbis.GetSamplesShortInterleaved (vorbisStruct,
                                                                   vorbisInfo.channels,
                                                                   temp_data,
                                                                   BufferSize - readData)
                        * vorbisInfo.channels;

                if (readCall > 0)
                {
                    System.Buffer.BlockCopy(temp_data,0,data,readData*2,readCall*2);
                    readData += readCall;
                }
                else
                    break;
            }
            
            // If we actually got data, buffer it into OpenAL.
            if (readData > 0) {
                AL.BufferData<short> (buffer,
                    (vorbisInfo.channels == 2) ? ALFormat.Stereo16 : ALFormat.Mono16,
                    data,
                    readData * sizeof(short), //* vorbisInfo.channels,
                    (int)vorbisInfo.sample_rate
                );
            } else {
                isFinished = true;
            }
        }
        
        #endregion

    }
}

