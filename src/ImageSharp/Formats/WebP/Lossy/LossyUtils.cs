// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming
namespace SixLabors.ImageSharp.Formats.Experimental.WebP.Lossy
{
    internal static class LossyUtils
    {
        public static void DC16(Span<byte> dst, Span<byte> yuv, int offset)
        {
            int offsetMinus1 = offset - 1;
            int offsetMinusBps = offset - WebPConstants.Bps;
            int dc = 16;
            for (int j = 0; j < 16; ++j)
            {
                // DC += dst[-1 + j * BPS] + dst[j - BPS];
                dc += yuv[offsetMinus1 + (j * WebPConstants.Bps)] + yuv[offsetMinusBps + j];
            }

            Put16(dc >> 5, dst);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void TM16(Span<byte> dst, Span<byte> yuv, int offset)
        {
            TrueMotion(dst, yuv, offset, 16);
        }

        public static void VE16(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // vertical
            Span<byte> src = yuv.Slice(offset - WebPConstants.Bps, 16);
            for (int j = 0; j < 16; ++j)
            {
                // memcpy(dst + j * BPS, dst - BPS, 16);
                src.CopyTo(dst.Slice(j * WebPConstants.Bps));
            }
        }

        public static void HE16(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // horizontal
            offset--;
            for (int j = 16; j > 0; --j)
            {
                // memset(dst, dst[-1], 16);
                byte v = yuv[offset];
                Memset(dst, v, 0, 16);
                offset += WebPConstants.Bps;
                dst = dst.Slice(WebPConstants.Bps);
            }
        }

        public static void DC16NoTop(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // DC with top samples not available.
            int dc = 8;
            for (int j = 0; j < 16; ++j)
            {
                // DC += dst[-1 + j * BPS];
                dc += yuv[-1 + (j * WebPConstants.Bps) + offset];
            }

            Put16(dc >> 4, dst);
        }

        public static void DC16NoLeft(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // DC with left samples not available.
            int dc = 8;
            for (int i = 0; i < 16; ++i)
            {
                // DC += dst[i - BPS];
                dc += yuv[i - WebPConstants.Bps + offset];
            }

            Put16(dc >> 4, dst);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void DC16NoTopLeft(Span<byte> dst)
        {
            // DC with no top and left samples.
            Put16(0x80, dst);
        }

        public static void DC8uv(Span<byte> dst, Span<byte> yuv, int offset)
        {
            int dc0 = 8;
            int offsetMinus1 = offset - 1;
            int offsetMinusBps = offset - WebPConstants.Bps;
            for (int i = 0; i < 8; ++i)
            {
                // dc0 += dst[i - BPS] + dst[-1 + i * BPS];
                dc0 += yuv[offsetMinusBps + i] + yuv[offsetMinus1 + (i * WebPConstants.Bps)];
            }

            Put8x8uv((byte)(dc0 >> 4), dst);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void TM8uv(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // TrueMotion
            TrueMotion(dst, yuv, offset, 8);
        }

        public static void VE8uv(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // vertical
            Span<byte> src = yuv.Slice(offset - WebPConstants.Bps, 8);

            int endIdx = 8 * WebPConstants.Bps;
            for (int j = 0; j < endIdx; j += WebPConstants.Bps)
            {
                // memcpy(dst + j * BPS, dst - BPS, 8);
                src.CopyTo(dst.Slice(j));
            }
        }

        public static void HE8uv(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // horizontal
            offset--;
            for (int j = 0; j < 8; ++j)
            {
                // memset(dst, dst[-1], 8);
                // dst += BPS;
                byte v = yuv[offset];
                Memset(dst, v, 0, 8);
                dst = dst.Slice(WebPConstants.Bps);
                offset += WebPConstants.Bps;
            }
        }

        public static void DC8uvNoTop(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // DC with no top samples.
            int dc0 = 4;
            int offsetMinusOne = offset - 1;
            int endIdx = 8 * WebPConstants.Bps;
            for (int i = 0; i < endIdx; i += WebPConstants.Bps)
            {
                // dc0 += dst[-1 + i * BPS];
                dc0 += yuv[offsetMinusOne + i];
            }

            Put8x8uv((byte)(dc0 >> 3), dst);
        }

        public static void DC8uvNoLeft(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // DC with no left samples.
            int offsetMinusBps = offset - WebPConstants.Bps;
            int dc0 = 4;
            for (int i = 0; i < 8; ++i)
            {
                // dc0 += dst[i - BPS];
                dc0 += yuv[offsetMinusBps + i];
            }

            Put8x8uv((byte)(dc0 >> 3), dst);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void DC8uvNoTopLeft(Span<byte> dst)
        {
            // DC with nothing.
            Put8x8uv(0x80, dst);
        }

        public static void DC4(Span<byte> dst, Span<byte> yuv, int offset)
        {
            int dc = 4;
            int offsetMinusBps = offset - WebPConstants.Bps;
            int offsetMinusOne = offset - 1;
            for (int i = 0; i < 4; ++i)
            {
                dc += yuv[offsetMinusBps + i] + yuv[offsetMinusOne + (i * WebPConstants.Bps)];
            }

            dc >>= 3;
            int endIndx = 4 * WebPConstants.Bps;
            for (int i = 0; i < endIndx; i += WebPConstants.Bps)
            {
                Memset(dst, (byte)dc, i, 4);
            }
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void TM4(Span<byte> dst, Span<byte> yuv, int offset)
        {
            TrueMotion(dst, yuv, offset, 4);
        }

        public static void VE4(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // vertical
            int topOffset = offset - WebPConstants.Bps;
            byte[] vals =
            {
                Avg3(yuv[topOffset - 1], yuv[topOffset], yuv[topOffset + 1]),
                Avg3(yuv[topOffset], yuv[topOffset + 1], yuv[topOffset + 2]),
                Avg3(yuv[topOffset + 1], yuv[topOffset + 2], yuv[topOffset + 3]),
                Avg3(yuv[topOffset + 2], yuv[topOffset + 3], yuv[topOffset + 4])
            };

            int endIdx = 4 * WebPConstants.Bps;
            for (int i = 0; i < endIdx; i += WebPConstants.Bps)
            {
                vals.CopyTo(dst.Slice(i));
            }
        }

        public static void HE4(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // horizontal
            int offsetMinusOne = offset - 1;
            byte a = yuv[offsetMinusOne - WebPConstants.Bps];
            byte b = yuv[offsetMinusOne];
            byte c = yuv[offsetMinusOne + WebPConstants.Bps];
            byte d = yuv[offsetMinusOne + (2 * WebPConstants.Bps)];
            byte e = yuv[offsetMinusOne + (3 * WebPConstants.Bps)];
            uint val = 0x01010101U * Avg3(a, b, c);
            BinaryPrimitives.WriteUInt32BigEndian(dst, val);
            val = 0x01010101U * Avg3(b, c, d);
            BinaryPrimitives.WriteUInt32BigEndian(dst.Slice(WebPConstants.Bps), val);
            val = 0x01010101U * Avg3(c, d, e);
            BinaryPrimitives.WriteUInt32BigEndian(dst.Slice(2 * WebPConstants.Bps), val);
            val = 0x01010101U * Avg3(d, e, e);
            BinaryPrimitives.WriteUInt32BigEndian(dst.Slice(3 * WebPConstants.Bps), val);
        }

        public static void RD4(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // Down-right
            int offsetMinusOne = offset - 1;
            byte i = yuv[offsetMinusOne];
            byte j = yuv[offsetMinusOne + (1 * WebPConstants.Bps)];
            byte k = yuv[offsetMinusOne + (2 * WebPConstants.Bps)];
            byte l = yuv[offsetMinusOne + (3 * WebPConstants.Bps)];
            byte x = yuv[offsetMinusOne - WebPConstants.Bps];
            byte a = yuv[offset - WebPConstants.Bps];
            byte b = yuv[offset + 1 - WebPConstants.Bps];
            byte c = yuv[offset + 2 - WebPConstants.Bps];
            byte d = yuv[offset + 3 - WebPConstants.Bps];

            Dst(dst, 0, 3, Avg3(j, k, l));
            byte ijk = Avg3(i, j, k);
            Dst(dst, 1, 3, ijk);
            Dst(dst, 0, 2, ijk);
            byte xij = Avg3(x, i, j);
            Dst(dst, 2, 3, xij);
            Dst(dst, 1, 2, xij);
            Dst(dst, 0, 1, xij);
            byte axi = Avg3(a, x, i);
            Dst(dst, 3, 3, axi);
            Dst(dst, 2, 2, axi);
            Dst(dst, 1, 1, axi);
            Dst(dst, 0, 0, axi);
            byte bax = Avg3(b, a, x);
            Dst(dst, 3, 2, bax);
            Dst(dst, 2, 1, bax);
            Dst(dst, 1, 0, bax);
            byte cba = Avg3(c, b, a);
            Dst(dst, 3, 1, cba);
            Dst(dst, 2, 0, cba);
            Dst(dst, 3, 0, Avg3(d, c, b));
        }

        public static void VR4(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // Vertical-Right
            int offsetMinusOne = offset - 1;
            byte i = yuv[offsetMinusOne];
            byte j = yuv[offsetMinusOne + (1 * WebPConstants.Bps)];
            byte k = yuv[offsetMinusOne + (2 * WebPConstants.Bps)];
            byte x = yuv[offsetMinusOne - WebPConstants.Bps];
            byte a = yuv[offset - WebPConstants.Bps];
            byte b = yuv[offset + 1 - WebPConstants.Bps];
            byte c = yuv[offset + 2 - WebPConstants.Bps];
            byte d = yuv[offset + 3 - WebPConstants.Bps];

            byte xa = Avg2(x, a);
            Dst(dst, 0, 0, xa);
            Dst(dst, 1, 2, xa);
            byte ab = Avg2(a, b);
            Dst(dst, 1, 0, ab);
            Dst(dst, 2, 2, ab);
            byte bc = Avg2(b, c);
            Dst(dst, 2, 0, bc);
            Dst(dst, 3, 2, bc);
            Dst(dst, 3, 0, Avg2(c, d));
            Dst(dst, 0, 3, Avg3(k, j, i));
            Dst(dst, 0, 2, Avg3(j, i, x));
            byte ixa = Avg3(i, x, a);
            Dst(dst, 0, 1, ixa);
            Dst(dst, 1, 3, ixa);
            byte xab = Avg3(x, a, b);
            Dst(dst, 1, 1, xab);
            Dst(dst, 2, 3, xab);
            byte abc = Avg3(a, b, c);
            Dst(dst, 2, 1, abc);
            Dst(dst, 3, 3, abc);
            Dst(dst, 3, 1, Avg3(b, c, d));
        }

        public static void LD4(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // Down-Left
            byte a = yuv[offset - WebPConstants.Bps];
            byte b = yuv[offset + 1 - WebPConstants.Bps];
            byte c = yuv[offset + 2 - WebPConstants.Bps];
            byte d = yuv[offset + 3 - WebPConstants.Bps];
            byte e = yuv[offset + 4 - WebPConstants.Bps];
            byte f = yuv[offset + 5 - WebPConstants.Bps];
            byte g = yuv[offset + 6 - WebPConstants.Bps];
            byte h = yuv[offset + 7 - WebPConstants.Bps];

            Dst(dst, 0, 0, Avg3(a, b, c));
            byte bcd = Avg3(b, c, d);
            Dst(dst, 1, 0, bcd);
            Dst(dst, 0, 1, bcd);
            byte cde = Avg3(c, d, e);
            Dst(dst, 2, 0, cde);
            Dst(dst, 1, 1, cde);
            Dst(dst, 0, 2, cde);
            byte def = Avg3(d, e, f);
            Dst(dst, 3, 0, def);
            Dst(dst, 2, 1, def);
            Dst(dst, 1, 2, def);
            Dst(dst, 0, 3, def);
            byte efg = Avg3(e, f, g);
            Dst(dst, 3, 1, efg);
            Dst(dst, 2, 2, efg);
            Dst(dst, 1, 3, efg);
            byte fgh = Avg3(f, g, h);
            Dst(dst, 3, 2, fgh);
            Dst(dst, 2, 3, fgh);
            Dst(dst, 3, 3, Avg3(g, h, h));
        }

        public static void VL4(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // Vertical-Left
            byte a = yuv[offset - WebPConstants.Bps];
            byte b = yuv[offset + 1 - WebPConstants.Bps];
            byte c = yuv[offset + 2 - WebPConstants.Bps];
            byte d = yuv[offset + 3 - WebPConstants.Bps];
            byte e = yuv[offset + 4 - WebPConstants.Bps];
            byte f = yuv[offset + 5 - WebPConstants.Bps];
            byte g = yuv[offset + 6 - WebPConstants.Bps];
            byte h = yuv[offset + 7 - WebPConstants.Bps];

            Dst(dst, 0, 0, Avg2(a, b));
            byte bc = Avg2(b, c);
            Dst(dst, 1, 0, bc);
            Dst(dst, 0, 2, bc);
            byte cd = Avg2(c, d);
            Dst(dst, 2, 0, cd);
            Dst(dst, 1, 2, cd);
            byte de = Avg2(d, e);
            Dst(dst, 3, 0, de);
            Dst(dst, 2, 2, de);
            Dst(dst, 0, 1, Avg3(a, b, c));
            byte bcd = Avg3(b, c, d);
            Dst(dst, 1, 1, bcd);
            Dst(dst, 0, 3, bcd);
            byte cde = Avg3(c, d, e);
            Dst(dst, 2, 1, cde);
            Dst(dst, 1, 3, cde);
            byte def = Avg3(d, e, f);
            Dst(dst, 3, 1, def);
            Dst(dst, 2, 3, def);
            Dst(dst, 3, 2, Avg3(e, f, g));
            Dst(dst, 3, 3, Avg3(f, g, h));
        }

        public static void HD4(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // Horizontal-Down
            byte i = yuv[offset - 1];
            byte j = yuv[offset - 1 + (1 * WebPConstants.Bps)];
            byte k = yuv[offset - 1 + (2 * WebPConstants.Bps)];
            byte l = yuv[offset - 1 + (3 * WebPConstants.Bps)];
            byte x = yuv[offset - 1 - WebPConstants.Bps];
            byte a = yuv[offset - WebPConstants.Bps];
            byte b = yuv[offset + 1 - WebPConstants.Bps];
            byte c = yuv[offset + 2 - WebPConstants.Bps];

            byte ix = Avg2(i, x);
            Dst(dst, 0, 0, ix);
            Dst(dst, 2, 1, ix);
            byte ji = Avg2(j, i);
            Dst(dst, 0, 1, ji);
            Dst(dst, 2, 2, ji);
            byte kj = Avg2(k, j);
            Dst(dst, 0, 2, kj);
            Dst(dst, 2, 3, kj);
            Dst(dst, 0, 3, Avg2(l, k));
            Dst(dst, 3, 0, Avg3(a, b, c));
            Dst(dst, 2, 0, Avg3(x, a, b));
            byte ixa = Avg3(i, x, a);
            Dst(dst, 1, 0, ixa);
            Dst(dst, 3, 1, ixa);
            byte jix = Avg3(j, i, x);
            Dst(dst, 1, 1, jix);
            Dst(dst, 3, 2, jix);
            byte kji = Avg3(k, j, i);
            Dst(dst, 1, 2, kji);
            Dst(dst, 3, 3, kji);
            Dst(dst, 1, 3, Avg3(l, k, j));
        }

        public static void HU4(Span<byte> dst, Span<byte> yuv, int offset)
        {
            // Horizontal-Up
            byte i = yuv[offset - 1];
            byte j = yuv[offset - 1 + (1 * WebPConstants.Bps)];
            byte k = yuv[offset - 1 + (2 * WebPConstants.Bps)];
            byte l = yuv[offset - 1 + (3 * WebPConstants.Bps)];

            Dst(dst, 0, 0, Avg2(i, j));
            byte jk = Avg2(j, k);
            Dst(dst, 2, 0, jk);
            Dst(dst, 0, 1, jk);
            byte kl = Avg2(k, l);
            Dst(dst, 2, 1, kl);
            Dst(dst, 0, 2, kl);
            Dst(dst, 1, 0, Avg3(i, j, k));
            byte jkl = Avg3(j, k, l);
            Dst(dst, 3, 0, jkl);
            Dst(dst, 1, 1, jkl);
            byte kll = Avg3(k, l, l);
            Dst(dst, 3, 1, kll);
            Dst(dst, 1, 2, kll);
            Dst(dst, 3, 2, l);
            Dst(dst, 2, 2, l);
            Dst(dst, 0, 3, l);
            Dst(dst, 1, 3, l);
            Dst(dst, 2, 3, l);
            Dst(dst, 3, 3, l);
        }

        /// <summary>
        /// Paragraph 14.3: Implementation of the Walsh-Hadamard transform inversion.
        /// </summary>
        public static void TransformWht(Span<short> input, Span<short> output)
        {
            var tmp = new int[16];
            for (int i = 0; i < 4; ++i)
            {
                int iPlus4 = 4 + i;
                int iPlus8 = 8 + i;
                int iPlus12 = 12 + i;
                int a0 = input[i] + input[iPlus12];
                int a1 = input[iPlus4] + input[iPlus8];
                int a2 = input[iPlus4] - input[iPlus8];
                int a3 = input[i] - input[iPlus12];
                tmp[i] = a0 + a1;
                tmp[iPlus8] = a0 - a1;
                tmp[iPlus4] = a3 + a2;
                tmp[iPlus12] = a3 - a2;
            }

            int outputOffset = 0;
            for (int i = 0; i < 4; ++i)
            {
                int imul4 = i * 4;
                int dc = tmp[0 + imul4] + 3;
                int a0 = dc + tmp[3 + imul4];
                int a1 = tmp[1 + imul4] + tmp[2 + imul4];
                int a2 = tmp[1 + imul4] - tmp[2 + imul4];
                int a3 = dc - tmp[3 + imul4];
                output[outputOffset + 0] = (short)((a0 + a1) >> 3);
                output[outputOffset + 16] = (short)((a3 + a2) >> 3);
                output[outputOffset + 32] = (short)((a0 - a1) >> 3);
                output[outputOffset + 48] = (short)((a3 - a2) >> 3);
                outputOffset += 64;
            }
        }

        public static void TransformTwo(Span<short> src, Span<byte> dst)
        {
            TransformOne(src, dst);
            TransformOne(src.Slice(16), dst.Slice(4));
        }

        public static void TransformOne(Span<short> src, Span<byte> dst)
        {
            Span<int> tmp = stackalloc int[4 * 4];
            int tmpOffset = 0;
            for (int srcOffset = 0; srcOffset < 4; srcOffset++)
            {
                // vertical pass
                int srcOffsetPlus4 = srcOffset + 4;
                int srcOffsetPlus8 = srcOffset + 8;
                int srcOffsetPlus12 = srcOffset + 12;
                int a = src[srcOffset] + src[srcOffsetPlus8];
                int b = src[srcOffset] - src[srcOffsetPlus8];
                int c = Mul2(src[srcOffsetPlus4]) - Mul1(src[srcOffsetPlus12]);
                int d = Mul1(src[srcOffsetPlus4]) + Mul2(src[srcOffsetPlus12]);
                tmp[tmpOffset++] = a + d;
                tmp[tmpOffset++] = b + c;
                tmp[tmpOffset++] = b - c;
                tmp[tmpOffset++] = a - d;
            }

            // Each pass is expanding the dynamic range by ~3.85 (upper bound).
            // The exact value is (2. + (20091 + 35468) / 65536).
            // After the second pass, maximum interval is [-3794, 3794], assuming
            // an input in [-2048, 2047] interval. We then need to add a dst value in the [0, 255] range.
            // In the worst case scenario, the input to clip_8b() can be as large as [-60713, 60968].
            tmpOffset = 0;
            for (int i = 0; i < 4; ++i)
            {
                // horizontal pass
                int tmpOffsetPlus4 = tmpOffset + 4;
                int tmpOffsetPlus8 = tmpOffset + 8;
                int tmpOffsetPlus12 = tmpOffset + 12;
                int dc = tmp[tmpOffset] + 4;
                int a = dc + tmp[tmpOffsetPlus8];
                int b = dc - tmp[tmpOffsetPlus8];
                int c = Mul2(tmp[tmpOffsetPlus4]) - Mul1(tmp[tmpOffsetPlus12]);
                int d = Mul1(tmp[tmpOffsetPlus4]) + Mul2(tmp[tmpOffsetPlus12]);
                Store(dst, 0, 0, a + d);
                Store(dst, 1, 0, b + c);
                Store(dst, 2, 0, b - c);
                Store(dst, 3, 0, a - d);
                tmpOffset++;
                dst = dst.Slice(WebPConstants.Bps);
            }
        }

        public static void TransformDc(Span<short> src, Span<byte> dst)
        {
            int dc = src[0] + 4;
            for (int j = 0; j < 4; ++j)
            {
                for (int i = 0; i < 4; ++i)
                {
                    Store(dst, i, j, dc);
                }
            }
        }

        // Simplified transform when only src[0], src[1] and src[4] are non-zero
        public static void TransformAc3(Span<short> src, Span<byte> dst)
        {
            int a = src[0] + 4;
            int c4 = Mul2(src[4]);
            int d4 = Mul1(src[4]);
            int c1 = Mul2(src[1]);
            int d1 = Mul1(src[1]);
            Store2(dst, 0, a + d4, d1, c1);
            Store2(dst, 1, a + c4, d1, c1);
            Store2(dst, 2, a - c4, d1, c1);
            Store2(dst, 3, a - d4, d1, c1);
        }

        public static void TransformUv(Span<short> src, Span<byte> dst)
        {
            TransformTwo(src.Slice(0 * 16), dst);
            TransformTwo(src.Slice(2 * 16), dst.Slice(4 * WebPConstants.Bps));
        }

        public static void TransformDcuv(Span<short> src, Span<byte> dst)
        {
            if (src[0 * 16] != 0)
            {
                TransformDc(src.Slice(0 * 16), dst);
            }

            if (src[1 * 16] != 0)
            {
                TransformDc(src.Slice(1 * 16), dst.Slice(4));
            }

            if (src[2 * 16] != 0)
            {
                TransformDc(src.Slice(2 * 16), dst.Slice(4 * WebPConstants.Bps));
            }

            if (src[3 * 16] != 0)
            {
                TransformDc(src.Slice(3 * 16), dst.Slice((4 * WebPConstants.Bps) + 4));
            }
        }

        // Simple In-loop filtering (Paragraph 15.2)
        public static void SimpleVFilter16(Span<byte> p, int offset, int stride, int thresh)
        {
            int thresh2 = (2 * thresh) + 1;
            int end = 16 + offset;
            for (int i = offset; i < end; ++i)
            {
                if (NeedsFilter(p, i, stride, thresh2))
                {
                    DoFilter2(p, i, stride);
                }
            }
        }

        public static void SimpleHFilter16(Span<byte> p, int offset, int stride, int thresh)
        {
            int thresh2 = (2 * thresh) + 1;
            int end = offset + (16 * stride);
            for (int i = offset; i < end; i += stride)
            {
                if (NeedsFilter(p, i, 1, thresh2))
                {
                    DoFilter2(p, i, 1);
                }
            }
        }

        public static void SimpleVFilter16i(Span<byte> p, int offset, int stride, int thresh)
        {
            for (int k = 3; k > 0; --k)
            {
                offset += 4 * stride;
                SimpleVFilter16(p, offset,  stride, thresh);
            }
        }

        public static void SimpleHFilter16i(Span<byte> p, int offset, int stride, int thresh)
        {
            for (int k = 3; k > 0; --k)
            {
                offset += 4;
                SimpleHFilter16(p, offset, stride, thresh);
            }
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void VFilter16(Span<byte> p, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            FilterLoop26(p, offset, stride, 1, 16, thresh, ithresh, hevThresh);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void HFilter16(Span<byte> p, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            FilterLoop26(p, offset, 1, stride, 16, thresh, ithresh, hevThresh);
        }

        public static void VFilter16i(Span<byte> p, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            for (int k = 3; k > 0; --k)
            {
                offset += 4 * stride;
                FilterLoop24(p, offset, stride, 1, 16, thresh, ithresh, hevThresh);
            }
        }

        public static void HFilter16i(Span<byte> p, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            for (int k = 3; k > 0; --k)
            {
                offset += 4;
                FilterLoop24(p, offset, 1, stride, 16, thresh, ithresh, hevThresh);
            }
        }

        // 8-pixels wide variant, for chroma filtering.
        [MethodImpl(InliningOptions.ShortMethod)]
        public static void VFilter8(Span<byte> u, Span<byte> v, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            FilterLoop26(u, offset, stride, 1, 8, thresh, ithresh, hevThresh);
            FilterLoop26(v, offset, stride, 1, 8, thresh, ithresh, hevThresh);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void HFilter8(Span<byte> u, Span<byte> v, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            FilterLoop26(u, offset, 1, stride, 8, thresh, ithresh, hevThresh);
            FilterLoop26(v, offset, 1, stride, 8, thresh, ithresh, hevThresh);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void VFilter8i(Span<byte> u, Span<byte> v, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            int offset4mulstride = offset + (4 * stride);
            FilterLoop24(u, offset4mulstride, stride, 1, 8, thresh, ithresh, hevThresh);
            FilterLoop24(v, offset4mulstride, stride, 1, 8, thresh, ithresh, hevThresh);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void HFilter8i(Span<byte> u, Span<byte> v, int offset, int stride, int thresh, int ithresh, int hevThresh)
        {
            int offsetPlus4 = offset + 4;
            FilterLoop24(u, offsetPlus4, 1, stride, 8, thresh, ithresh, hevThresh);
            FilterLoop24(v, offsetPlus4, 1, stride, 8, thresh, ithresh, hevThresh);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static uint LoadUv(byte u, byte v)
        {
            // We process u and v together stashed into 32bit(16bit each).
            return (uint)(u | (v << 16));
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void YuvToBgr(int y, int u, int v, Span<byte> bgr)
        {
            bgr[0] = (byte)YuvToB(y, u);
            bgr[1] = (byte)YuvToG(y, u, v);
            bgr[2] = (byte)YuvToR(y, v);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static int YuvToB(int y, int u)
        {
            return Clip8(MultHi(y, 19077) + MultHi(u, 33050) - 17685);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static int YuvToG(int y, int u, int v)
        {
            return Clip8(MultHi(y, 19077) - MultHi(u, 6419) - MultHi(v, 13320) + 8708);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static int YuvToR(int y, int v)
        {
            return Clip8(MultHi(y, 19077) + MultHi(v, 26149) - 14234);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static byte Avg2(byte a, byte b)
        {
            return (byte)((a + b + 1) >> 1);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static byte Avg3(byte a, byte b, byte c)
        {
            return (byte)((a + (2 * b) + c + 2) >> 2);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static void Dst(Span<byte> dst, int x, int y, byte v)
        {
            dst[x + (y * WebPConstants.Bps)] = v;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        public static byte Clip8B(int v)
        {
            return (byte)((v & ~0xff) == 0 ? v : (v < 0) ? 0 : 255);
        }

        // Cost of coding one event with probability 'proba'.
        public static int Vp8BitCost(int bit, byte proba)
        {
            return bit == 0 ? WebPLookupTables.Vp8EntropyCost[proba] : WebPLookupTables.Vp8EntropyCost[255 - proba];
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static void Put16(int v, Span<byte> dst)
        {
            for (int j = 0; j < 16; ++j)
            {
                Memset(dst.Slice(j * WebPConstants.Bps), (byte)v, 0, 16);
            }
        }

        private static void TrueMotion(Span<byte> dst, Span<byte> yuv, int offset, int size)
        {
            // For information about how true motion works, see rfc6386, page 52. ff and section 20.14.
            int topOffset = offset - WebPConstants.Bps;
            Span<byte> top = yuv.Slice(topOffset);
            byte p = yuv[topOffset - 1];
            int leftOffset = offset - 1;
            byte left = yuv[leftOffset];
            for (int y = 0; y < size; ++y)
            {
                for (int x = 0; x < size; ++x)
                {
                    dst[x] = (byte)Clamp255(left + top[x] - p);
                }

                leftOffset += WebPConstants.Bps;
                left = yuv[leftOffset];
                dst = dst.Slice(WebPConstants.Bps);
            }
        }

        // Complex In-loop filtering (Paragraph 15.3)
        private static void FilterLoop24(
            Span<byte> p,
            int offset,
            int hStride,
            int vStride,
            int size,
            int thresh,
            int ithresh,
            int hevThresh)
        {
            int thresh2 = (2 * thresh) + 1;
            while (size-- > 0)
            {
                if (NeedsFilter2(p, offset,  hStride, thresh2, ithresh))
                {
                    if (Hev(p, offset, hStride, hevThresh))
                    {
                        DoFilter2(p, offset, hStride);
                    }
                    else
                    {
                        DoFilter4(p, offset, hStride);
                    }
                }

                offset += vStride;
            }
        }

        private static void FilterLoop26(
            Span<byte> p,
            int offset,
            int hStride,
            int vStride,
            int size,
            int thresh,
            int ithresh,
            int hevThresh)
        {
            int thresh2 = (2 * thresh) + 1;
            while (size-- > 0)
            {
                if (NeedsFilter2(p, offset, hStride, thresh2, ithresh))
                {
                    if (Hev(p, offset, hStride, hevThresh))
                    {
                        DoFilter2(p, offset, hStride);
                    }
                    else
                    {
                        DoFilter6(p, offset, hStride);
                    }
                }

                offset += vStride;
            }
        }

        private static void DoFilter2(Span<byte> p, int offset, int step)
        {
            // 4 pixels in, 2 pixels out.
            int p1 = p[offset - (2 * step)];
            int p0 = p[offset - step];
            int q0 = p[offset];
            int q1 = p[offset + step];
            int a = (3 * (q0 - p0)) + WebPLookupTables.Sclip1[p1 - q1];
            int a1 = WebPLookupTables.Sclip2[(a + 4) >> 3];
            int a2 = WebPLookupTables.Sclip2[(a + 3) >> 3];
            p[offset - step] = WebPLookupTables.Clip1[p0 + a2];
            p[offset] = WebPLookupTables.Clip1[q0 - a1];
        }

        private static void DoFilter4(Span<byte> p, int offset, int step)
        {
            // 4 pixels in, 4 pixels out.
            int offsetMinus2Step = offset - (2 * step);
            int p1 = p[offsetMinus2Step];
            int p0 = p[offset - step];
            int q0 = p[offset];
            int q1 = p[offset + step];
            int a = 3 * (q0 - p0);
            int a1 = WebPLookupTables.Sclip2[(a + 4) >> 3];
            int a2 = WebPLookupTables.Sclip2[(a + 3) >> 3];
            int a3 = (a1 + 1) >> 1;
            p[offsetMinus2Step] = WebPLookupTables.Clip1[p1 + a3];
            p[offset - step] = WebPLookupTables.Clip1[p0 + a2];
            p[offset] = WebPLookupTables.Clip1[q0 - a1];
            p[offset + step] = WebPLookupTables.Clip1[q1 - a3];
        }

        private static void DoFilter6(Span<byte> p, int offset, int step)
        {
            // 6 pixels in, 6 pixels out.
            int step2 = 2 * step;
            int step3 = 3 * step;
            int offsetMinusStep = offset - step;
            int p2 = p[offset - step3];
            int p1 = p[offset - step2];
            int p0 = p[offsetMinusStep];
            int q0 = p[offset];
            int q1 = p[offset + step];
            int q2 = p[offset + step2];
            int a = WebPLookupTables.Sclip1[(3 * (q0 - p0)) + WebPLookupTables.Sclip1[p1 - q1]];

            // a is in [-128,127], a1 in [-27,27], a2 in [-18,18] and a3 in [-9,9]
            int a1 = ((27 * a) + 63) >> 7;  // eq. to ((3 * a + 7) * 9) >> 7
            int a2 = ((18 * a) + 63) >> 7;  // eq. to ((2 * a + 7) * 9) >> 7
            int a3 = ((9 * a) + 63) >> 7;  // eq. to ((1 * a + 7) * 9) >> 7
            p[offset - step3] = WebPLookupTables.Clip1[p2 + a3];
            p[offset - step2] = WebPLookupTables.Clip1[p1 + a2];
            p[offsetMinusStep] = WebPLookupTables.Clip1[p0 + a1];
            p[offset] = WebPLookupTables.Clip1[q0 - a1];
            p[offset + step] = WebPLookupTables.Clip1[q1 - a2];
            p[offset + step2] = WebPLookupTables.Clip1[q2 - a3];
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static bool NeedsFilter(Span<byte> p, int offset, int step, int t)
        {
            int p1 = p[offset + (-2 * step)];
            int p0 = p[offset - step];
            int q0 = p[offset];
            int q1 = p[offset + step];
            return ((4 * WebPLookupTables.Abs0[p0 - q0]) + WebPLookupTables.Abs0[p1 - q1]) <= t;
        }

        private static bool NeedsFilter2(Span<byte> p, int offset, int step, int t, int it)
        {
            int step2 = 2 * step;
            int step3 = 3 * step;
            int p3 = p[offset - (4 * step)];
            int p2 = p[offset - step3];
            int p1 = p[offset - step2];
            int p0 = p[offset - step];
            int q0 = p[offset];
            int q1 = p[offset + step];
            int q2 = p[offset + step2];
            int q3 = p[offset + step3];
            if (((4 * WebPLookupTables.Abs0[p0 - q0]) + WebPLookupTables.Abs0[p1 - q1]) > t)
            {
                return false;
            }

            return WebPLookupTables.Abs0[p3 - p2] <= it && WebPLookupTables.Abs0[p2 - p1] <= it &&
                   WebPLookupTables.Abs0[p1 - p0] <= it && WebPLookupTables.Abs0[q3 - q2] <= it &&
                   WebPLookupTables.Abs0[q2 - q1] <= it && WebPLookupTables.Abs0[q1 - q0] <= it;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static bool Hev(Span<byte> p, int offset, int step, int thresh)
        {
            int p1 = p[offset - (2 * step)];
            int p0 = p[offset - step];
            int q0 = p[offset];
            int q1 = p[offset + step];
            return (WebPLookupTables.Abs0[p1 - p0] > thresh) || (WebPLookupTables.Abs0[q1 - q0] > thresh);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static int MultHi(int v, int coeff)
        {
            return (v * coeff) >> 8;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static void Store(Span<byte> dst, int x, int y, int v)
        {
            var index = x + (y * WebPConstants.Bps);
            dst[index] = Clip8B(dst[index] + (v >> 3));
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static void Store2(Span<byte> dst, int y, int dc, int d, int c)
        {
            Store(dst, 0, y, dc + d);
            Store(dst, 1, y, dc + c);
            Store(dst, 2, y, dc - c);
            Store(dst, 3, y, dc - d);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static int Mul1(int a)
        {
            return ((a * 20091) >> 16) + a;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static int Mul2(int a)
        {
            return (a * 35468) >> 16;
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static byte Clip8(int v)
        {
            int yuvMask = (256 << 6) - 1;
            return (byte)(((v & ~yuvMask) == 0) ? (v >> 6) : (v < 0) ? 0 : 255);
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static void Put8x8uv(byte value, Span<byte> dst)
        {
            int end = 8 * WebPConstants.Bps;
            for (int j = 0; j < end; j += WebPConstants.Bps)
            {
                // memset(dst + j * BPS, value, 8);
                Memset(dst, value, j, 8);
            }
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static void Memset(Span<byte> dst, byte value, int startIdx, int count)
        {
            int end = startIdx + count;
            for (int i = startIdx; i < end; i++)
            {
                dst[i] = value;
            }
        }

        [MethodImpl(InliningOptions.ShortMethod)]
        private static int Clamp255(int x)
        {
            return x < 0 ? 0 : (x > 255 ? 255 : x);
        }
    }
}