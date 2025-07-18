﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: This file contains the implementation of MatrixUtil, which 
//              provides matrix multiply code.
// 
//  
//
//

using System.Diagnostics;
using WpfInk.WindowsBase.System.Windows.Media;


namespace MS.Internal;

// MatrixTypes

internal static class MatrixUtil
{
    /// <summary>
    /// Multiplies two transformations, where the behavior is matrix1 *= matrix2.
    /// This code exists so that we can efficient combine matrices without copying
    /// the data around, since each matrix is 52 bytes.
    /// To reduce duplication and to ensure consistent behavior, this is the
    /// method which is used to implement Matrix * Matrix as well.
    /// </summary>
    internal static void MultiplyMatrix(ref Matrix matrix1, ref Matrix matrix2)
    {
        MatrixTypes type1 = matrix1._type;
        MatrixTypes type2 = matrix2._type;

        // Check for idents

        // If the second is ident, we can just return
        if (type2 == MatrixTypes.TRANSFORM_IS_IDENTITY)
        {
            return;
        }

        // If the first is ident, we can just copy the memory across.
        if (type1 == MatrixTypes.TRANSFORM_IS_IDENTITY)
        {
            matrix1 = matrix2;
            return;
        }

        // Optimize for translate case, where the second is a translate
        if (type2 == MatrixTypes.TRANSFORM_IS_TRANSLATION)
        {
            // 2 additions
            matrix1._offsetX += matrix2._offsetX;
            matrix1._offsetY += matrix2._offsetY;

            // If matrix 1 wasn't unknown we added a translation
            if (type1 != MatrixTypes.TRANSFORM_IS_UNKNOWN)
            {
                matrix1._type |= MatrixTypes.TRANSFORM_IS_TRANSLATION;
            }

            return;
        }

        // Check for the first value being a translate
        if (type1 == MatrixTypes.TRANSFORM_IS_TRANSLATION)
        {
            // Save off the old offsets
            double offsetX = matrix1._offsetX;
            double offsetY = matrix1._offsetY;

            // Copy the matrix
            matrix1 = matrix2;

            matrix1._offsetX = offsetX * matrix2._m11 + offsetY * matrix2._m21 + matrix2._offsetX;
            matrix1._offsetY = offsetX * matrix2._m12 + offsetY * matrix2._m22 + matrix2._offsetY;

            if (type2 == MatrixTypes.TRANSFORM_IS_UNKNOWN)
            {
                matrix1._type = MatrixTypes.TRANSFORM_IS_UNKNOWN;
            }
            else
            {
                matrix1._type = MatrixTypes.TRANSFORM_IS_SCALING | MatrixTypes.TRANSFORM_IS_TRANSLATION;
            }
            return;
        }

        // The following code combines the type of the transformations so that the high nibble
        // is "this"'s type, and the low nibble is mat's type.  This allows for a switch rather
        // than nested switches.

        // trans1._type |  trans2._type
        //  7  6  5  4   |  3  2  1  0
        int combinedType = ((int)type1 << 4) | (int)type2;

        switch (combinedType)
        {
            case 34:  // S * S
                // 2 multiplications
                matrix1._m11 *= matrix2._m11;
                matrix1._m22 *= matrix2._m22;
                return;

            case 35:  // S * S|T
                matrix1._m11 *= matrix2._m11;
                matrix1._m22 *= matrix2._m22;
                matrix1._offsetX = matrix2._offsetX;
                matrix1._offsetY = matrix2._offsetY;

                // Transform set to Translate and Scale
                matrix1._type = MatrixTypes.TRANSFORM_IS_TRANSLATION | MatrixTypes.TRANSFORM_IS_SCALING;
                return;

            case 50: // S|T * S
                matrix1._m11 *= matrix2._m11;
                matrix1._m22 *= matrix2._m22;
                matrix1._offsetX *= matrix2._m11;
                matrix1._offsetY *= matrix2._m22;
                return;

            case 51: // S|T * S|T
                matrix1._m11 *= matrix2._m11;
                matrix1._m22 *= matrix2._m22;
                matrix1._offsetX = matrix2._m11 * matrix1._offsetX + matrix2._offsetX;
                matrix1._offsetY = matrix2._m22 * matrix1._offsetY + matrix2._offsetY;
                return;
            case 36: // S * U
            case 52: // S|T * U
            case 66: // U * S
            case 67: // U * S|T
            case 68: // U * U
                matrix1 = new Matrix(
                    matrix1._m11 * matrix2._m11 + matrix1._m12 * matrix2._m21,
                    matrix1._m11 * matrix2._m12 + matrix1._m12 * matrix2._m22,

                    matrix1._m21 * matrix2._m11 + matrix1._m22 * matrix2._m21,
                    matrix1._m21 * matrix2._m12 + matrix1._m22 * matrix2._m22,

                    matrix1._offsetX * matrix2._m11 + matrix1._offsetY * matrix2._m21 + matrix2._offsetX,
                    matrix1._offsetX * matrix2._m12 + matrix1._offsetY * matrix2._m22 + matrix2._offsetY);
                return;
#if DEBUG
            default:
                Debug.Fail("Matrix multiply hit an invalid case: " + combinedType);
                break;
#endif
        }
    }        

    /// <summary>
    /// Applies an offset to the specified matrix in place.
    /// </summary>
    internal static void PrependOffset(
        ref Matrix matrix,
        double offsetX,
        double offsetY)
    {
        if (matrix._type == MatrixTypes.TRANSFORM_IS_IDENTITY)
        {
            matrix = new Matrix(1, 0, 0, 1, offsetX, offsetY);
            matrix._type = MatrixTypes.TRANSFORM_IS_TRANSLATION;
        }
        else
        {
            // 
            //  / 1   0   0 \       / m11   m12   0 \
            //  | 0   1   0 |   *   | m21   m22   0 |
            //  \ tx  ty  1 /       \  ox    oy   1 /
            //
            //       /   m11                  m12                     0 \
            //  =    |   m21                  m22                     0 |
            //       \   m11*tx+m21*ty+ox     m12*tx + m22*ty + oy    1 /
            //

            matrix._offsetX += matrix._m11 * offsetX + matrix._m21 * offsetY;
            matrix._offsetY += matrix._m12 * offsetX + matrix._m22 * offsetY;

            // It just gained a translate if was a scale transform. Identity transform is handled above.
            Debug.Assert(matrix._type != MatrixTypes.TRANSFORM_IS_IDENTITY);
            if (matrix._type != MatrixTypes.TRANSFORM_IS_UNKNOWN)
            {
                matrix._type |= MatrixTypes.TRANSFORM_IS_TRANSLATION;
            }
        }
    }
}