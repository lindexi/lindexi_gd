using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ShmSeg = System.UInt32;

namespace WercawchallwarnefeWhedurcachay;
internal class XShm
{
}

[StructLayout(LayoutKind.Sequential)]
unsafe struct XShmSegmentInfo
{
   public ShmSeg shmseg;    /* resource id */
   public int shmid;        /* kernel id */
   public char* shmaddr;    /* address in client */
   public bool readOnly;	/* how the server should attach it */

   public override string ToString()
   {
       return    $"XShmSegmentInfo {{ shmseg = {shmseg}, shmid = {shmid}, shmaddr = {new IntPtr(shmaddr).ToString("X")}, readOnly = {readOnly} }}";
   }
}