//using CPF.Linux;

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//using static CPF.Linux.XLib;

//namespace BujeeberehemnaNurgacolarje;

//public class X11WindowHelper
//{


//    public void ChangeWMAtoms(bool enable, params IntPtr[] atoms)
//    {
//        if (atoms.Length != 1 && atoms.Length != 2)
//            throw new ArgumentException();

//        //if (!_mapped)
//        //{
//        //    var newAtoms = new HashSet<IntPtr>(XGetWindowPropertyAsIntPtrArray(_x11.Display, _handle,
//        //        _x11.Atoms._NET_WM_STATE,
//        //        (IntPtr) Atom.XA_ATOM) ?? []);

//        //    foreach (var atom in atoms)
//        //        if (enable)
//        //            newAtoms.Add(atom);
//        //        else
//        //            newAtoms.Remove(atom);

//        //    XChangeProperty(_x11.Display, _handle, _x11.Atoms._NET_WM_STATE, (IntPtr) Atom.XA_ATOM, 32,
//        //        PropertyMode.Replace, newAtoms.ToArray(), newAtoms.Count);
//        //}

//        SendNetWMMessage(_x11.Atoms._NET_WM_STATE,
//            (IntPtr) (enable ? 1 : 0),
//            atoms[0],
//            atoms.Length > 1 ? atoms[1] : IntPtr.Zero,
//            atoms.Length > 2 ? atoms[2] : IntPtr.Zero,
//            atoms.Length > 3 ? atoms[3] : IntPtr.Zero
//        );
//    }

//}
