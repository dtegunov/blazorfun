using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Warp.Headers;
using System.IO;

namespace Warp.Tools
{
    public static class StreamMapReader
    {
        public static float[][] ReadMapFloat(BinaryReader reader, string path, int2 headerlessSliceDims, long headerlessOffset, Type headerlessType, bool isBigEndian, int[] layers = null, Stream stream = null, float[][] reuseBuffer = null)
        {
            MapHeader Header = null;
            Type ValueType = null;
            float[][] Data;

            BinaryReader Reader = reader;
            {
                Header = MapHeader.ReadFromFile(Reader, path, headerlessSliceDims, headerlessOffset, headerlessType);
                ValueType = Header.GetValueType();
                Data = reuseBuffer == null ? new float[layers == null ? Header.Dimensions.Z : layers.Length][] : reuseBuffer;

                int ReadBatchSize = Math.Min((int)Header.Dimensions.ElementsSlice(), 1 << 20);
                int ValueSize = (int)ImageFormatsHelper.SizeOf(ValueType);
                byte[] Bytes = new byte[ReadBatchSize * ValueSize];

                long ReaderDataStart = Reader.BaseStream.Position;

                for (int z = 0; z < Data.Length; z++)
                {
                    if (layers != null)
                        Reader.BaseStream.Seek(Header.Dimensions.ElementsSlice() * ImageFormatsHelper.SizeOf(ValueType) * layers[z] + ReaderDataStart, SeekOrigin.Begin);

                    if (reuseBuffer == null)
                        Data[z] = new float[(int)Header.Dimensions.ElementsSlice()];


                    unsafe
                    {
                        fixed (byte* BytesPtr = Bytes)
                        fixed (float* DataPtr = Data[z])
                        {
                            for (int b = 0; b < (int)Header.Dimensions.ElementsSlice(); b += ReadBatchSize)
                            {
                                int CurBatch = Math.Min(ReadBatchSize, (int)Header.Dimensions.ElementsSlice() - b);

                                Reader.Read(Bytes, 0, CurBatch * ValueSize);

                                if (isBigEndian)
                                {
                                    if (ValueType == typeof(short) || ValueType == typeof(ushort))
                                    {
                                        for (int i = 0; i < CurBatch * ValueSize / 2; i++)
                                            Array.Reverse(Bytes, i * 2, 2);
                                    }
                                    else if (ValueType == typeof(int) || ValueType == typeof(float))
                                    {
                                        for (int i = 0; i < CurBatch * ValueSize / 4; i++)
                                            Array.Reverse(Bytes, i * 4, 4);
                                    }
                                    else if (ValueType == typeof(double))
                                    {
                                        for (int i = 0; i < CurBatch * ValueSize / 8; i++)
                                            Array.Reverse(Bytes, i * 8, 8);
                                    }
                                }

                                float* DataP = DataPtr + b;

                                if (ValueType == typeof(byte))
                                {
                                    byte* BytesP = BytesPtr;
                                    for (int i = 0; i < CurBatch; i++)
                                        *DataP++ = (float)*BytesP++;
                                }
                                else if (ValueType == typeof(short))
                                {
                                    short* BytesP = (short*)BytesPtr;
                                    for (int i = 0; i < CurBatch; i++)
                                        *DataP++ = (float)*BytesP++;
                                }
                                else if (ValueType == typeof(ushort))
                                {
                                    ushort* BytesP = (ushort*)BytesPtr;
                                    for (int i = 0; i < CurBatch; i++)
                                        *DataP++ = (float)*BytesP++;
                                }
                                else if (ValueType == typeof(int))
                                {
                                    int* BytesP = (int*)BytesPtr;
                                    for (int i = 0; i < CurBatch; i++)
                                        *DataP++ = (float)*BytesP++;
                                }
                                else if (ValueType == typeof(float))
                                {
                                    float* BytesP = (float*)BytesPtr;
                                    for (int i = 0; i < CurBatch; i++)
                                        *DataP++ = *BytesP++;
                                }
                                else if (ValueType == typeof(double))
                                {
                                    double* BytesP = (double*)BytesPtr;
                                    for (int i = 0; i < CurBatch; i++)
                                        *DataP++ = (float)*BytesP++;
                                }
                            }
                        }
                    }
                }
            }

            return Data;
        }
    }
}
