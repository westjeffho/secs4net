using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Security.Permissions;
using System.Text;

namespace Secs4Net {
    [DebuggerDisplay("<{Format} [{Count}] { (Format==SecsFormat.List) ? string.Empty : ToString() ,nq}>")]
    public sealed class Item : MarshalByRefObject {
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService() {
            ILease lease = (ILease)base.InitializeLifetimeService();
            if (lease.CurrentState == LeaseState.Initial) {
                lease.InitialLeaseTime = TimeSpan.FromSeconds(10);
                lease.RenewOnCallTime = TimeSpan.FromSeconds(10);
            }
            return lease;
        }

        public SecsFormat Format { get; }
        public int Count { get; }

        public ReadOnlyCollection<Item> Items {
            get {
                if (_list == null) throw new InvalidOperationException("This is not a List.");
                return _list;
            }
        }

        public T Value<T>() {
            if (_value == null)
                throw new InvalidOperationException("This is a List.");

            if (_value is T)
                return (T)((ICloneable)_value).Clone();

            if (_value is T[])
                return ((T[])_value)[0];

            Type valueType = Nullable.GetUnderlyingType(typeof(T));
            if (valueType != null && _value.GetType().GetElementType() == valueType)
                return ((IEnumerable)_value).Cast<T>().FirstOrDefault();

            throw new SecsException("Access item value with wrong type.");
        }

        public RawData RawData => _rawBytes.Value;

        public override string ToString() => _sml.Value;

        /// <summary>
        /// if Format is List RawBytes is only header bytes.
        /// otherwise include header and data bytes.
        /// </summary>
        readonly Lazy<RawData> _rawBytes;
        readonly Lazy<string> _sml;
        readonly ReadOnlyCollection<Item> _list;  //  當Format為List時 _list才有值,否則為null
        readonly object _value;      //  當Format不為List時 _value才有值,否則為null;不是string就是Array   

        #region Constructor
        /// <summary>
        /// List
        /// </summary>
        Item(ReadOnlyCollection<Item> items) {
            Format = SecsFormat.List;
            Count = items.Count;
            _list = items;
            _sml = EmptySml;
            _rawBytes = Lazy.Create(() => {
                int _;
                return new RawData(Format.EncodeItem(Count, out _));
            });
        }

        /// <summary>
        /// U2,U4,U8
        /// I1,I2,I4,I8
        /// F4,F8
        /// Boolean
        /// </summary>
        Item(SecsFormat format, Array value, Lazy<string> sml) {
            Format = format;
            Count = value.Length;
            _value = value;
            _sml = sml;
            _rawBytes = Lazy.Create(() => {
                Array val = (Array)_value;
                int bytelength = Buffer.ByteLength(val);
                int headerLength;
                byte[] result = Format.EncodeItem(bytelength, out headerLength);
                Buffer.BlockCopy(val, 0, result, headerLength, bytelength);
                result.Reverse(headerLength, headerLength + bytelength, bytelength / val.Length);
                return new RawData(result);
            });
        }

        /// <summary>
        /// A,J
        /// </summary>
        Item(SecsFormat format, string value, Encoding encoder) {
            Format = format;
            Count = value.Length;
            _value = value;
            _sml = Lazy.Create(value);
            _rawBytes = Lazy.Create(() => {
                string str = (string)_value;
                int headerLength;
                byte[] result = Format.EncodeItem(str.Length, out headerLength);
                encoder.GetBytes(str, 0, str.Length, result, headerLength);
                return new RawData(result);
            });
        }

        /// <summary>
        /// Empty Item(none List)
        /// </summary>
        /// <param name="format"></param>
        /// <param name="value"></param>
        Item(SecsFormat format, ICloneable value) {
            Format = format;
            _value = value;
            _rawBytes = Lazy.Create(new RawData(new byte[] { (byte)((byte)Format | 1), 0 }));
            _sml = EmptySml;
        }
        #endregion

        #region Value Access Operator
        public static explicit operator string (Item item) => item.Value<string>();
        public static explicit operator byte (Item item) => item.Value<byte>();
        public static explicit operator sbyte (Item item) => item.Value<sbyte>();
        public static explicit operator ushort (Item item) => item.Value<ushort>();
        public static explicit operator short (Item item) => item.Value<short>();
        public static explicit operator uint (Item item) => item.Value<uint>();
        public static explicit operator int (Item item) => item.Value<int>();
        public static explicit operator ulong (Item item) => item.Value<ulong>();
        public static explicit operator long (Item item) => item.Value<long>();
        public static explicit operator float (Item item) => item.Value<float>();
        public static explicit operator double (Item item) => item.Value<double>();
        public static explicit operator bool (Item item) => item.Value<bool>();
        public static explicit operator byte? (Item item) => item.Value<byte?>();
        public static explicit operator sbyte? (Item item) => item.Value<sbyte?>();
        public static explicit operator ushort? (Item item) => item.Value<ushort?>();
        public static explicit operator short? (Item item) => item.Value<short?>();
        public static explicit operator uint? (Item item) => item.Value<uint?>();
        public static explicit operator int? (Item item) => item.Value<int?>();
        public static explicit operator ulong? (Item item) => item.Value<ulong?>();
        public static explicit operator long? (Item item) => item.Value<long?>();
        public static explicit operator float? (Item item) => item.Value<float?>();
        public static explicit operator double? (Item item) => item.Value<double?>();
        public static explicit operator bool? (Item item) => item.Value<bool?>();
        public static explicit operator byte[] (Item item) => item.Value<byte[]>();
        public static explicit operator sbyte[] (Item item) => item.Value<sbyte[]>();
        public static explicit operator ushort[] (Item item) => item.Value<ushort[]>();
        public static explicit operator short[] (Item item) => item.Value<short[]>();
        public static explicit operator uint[] (Item item) => item.Value<uint[]>();
        public static explicit operator int[] (Item item) => item.Value<int[]>();
        public static explicit operator ulong[] (Item item) => item.Value<ulong[]>();
        public static explicit operator long[] (Item item) => item.Value<long[]>();
        public static explicit operator float[] (Item item) => item.Value<float[]>();
        public static explicit operator double[] (Item item) => item.Value<double[]>();
        public static explicit operator bool[] (Item item) => item.Value<bool[]>();
        #endregion

        #region Factory Methods
        internal static Item L(IList<Item> items) => new Item(new ReadOnlyCollection<Item>(items));
        public static Item L(IEnumerable<Item> items) => items.Any() ? L(items.ToList()) : L();
        public static Item L(params Item[] items) => L(items.ToList());
        public static Item B(params byte[] value) => new Item(SecsFormat.Binary, value, new Lazy<string>(value.ToHexString));
        public static Item U1(params byte[] value) => new Item(SecsFormat.U1, value, new Lazy<string>(value.ToSmlString));
        public static Item U2(params ushort[] value) => new Item(SecsFormat.U2, value, new Lazy<string>(value.ToSmlString));
        public static Item U4(params uint[] value) => new Item(SecsFormat.U4, value, new Lazy<string>(value.ToSmlString));
        public static Item U8(params ulong[] value) => new Item(SecsFormat.U8, value, new Lazy<string>(value.ToSmlString));
        public static Item I1(params sbyte[] value) => new Item(SecsFormat.I1, value, new Lazy<string>(value.ToSmlString));
        public static Item I2(params short[] value) => new Item(SecsFormat.I2, value, new Lazy<string>(value.ToSmlString));
        public static Item I4(params int[] value) => new Item(SecsFormat.I4, value, new Lazy<string>(value.ToSmlString));
        public static Item I8(params long[] value) => new Item(SecsFormat.I8, value, new Lazy<string>(value.ToSmlString));
        public static Item F4(params float[] value) => new Item(SecsFormat.F4, value, new Lazy<string>(value.ToSmlString));
        public static Item F8(params double[] value) => new Item(SecsFormat.F8, value, new Lazy<string>(value.ToSmlString));
        public static Item Boolean(params bool[] value) => new Item(SecsFormat.Boolean, value, new Lazy<string>(value.ToSmlString));
        public static Item A(string value) => new Item(SecsFormat.ASCII, value, Encoding.ASCII);
        public static Item J(string value) => new Item(SecsFormat.JIS8, value, JIS8Encoding);
        #endregion

        #region Empty Item Factory
        public static Item L() => Empty_L;
        public static Item B() => Empty_Binary;
        public static Item U1() => Empty_U1;
        public static Item U2() => Empty_U2;
        public static Item U4() => Empty_U4;
        public static Item U8() => Empty_U8;
        public static Item I1() => Empty_I1;
        public static Item I2() => Empty_I2;
        public static Item I4() => Empty_I4;
        public static Item I8() => Empty_I8;
        public static Item F4() => Empty_F4;
        public static Item F8() => Empty_F8;
        public static Item Boolean() => Empty_Boolean;
        public static Item A() => Empty_A;
        public static Item J() => Empty_J;
        #endregion

        #region Share Object
        internal static readonly Encoding JIS8Encoding = Encoding.GetEncoding(50222);
        internal static readonly Lazy<string> EmptySml = Lazy.Create(string.Empty);
        static readonly Item Empty_L       = new Item(Array.AsReadOnly(new Item[0]));
        static readonly Item Empty_A       = new Item(SecsFormat.ASCII, string.Empty);
        static readonly Item Empty_J       = new Item(SecsFormat.JIS8, string.Empty);
        static readonly Item Empty_Boolean = new Item(SecsFormat.Boolean, new bool[0]);
        static readonly Item Empty_Binary  = new Item(SecsFormat.Binary, new byte[0]);
        static readonly Item Empty_U1      = new Item(SecsFormat.U1, new byte[0]);
        static readonly Item Empty_U2      = new Item(SecsFormat.U2, new ushort[0]);
        static readonly Item Empty_U4      = new Item(SecsFormat.U4, new uint[0]);
        static readonly Item Empty_U8      = new Item(SecsFormat.U8, new ulong[0]);
        static readonly Item Empty_I1      = new Item(SecsFormat.I1, new sbyte[0]);
        static readonly Item Empty_I2      = new Item(SecsFormat.I2, new short[0]);
        static readonly Item Empty_I4      = new Item(SecsFormat.I4, new int[0]);
        static readonly Item Empty_I8      = new Item(SecsFormat.I8, new long[0]);
        static readonly Item Empty_F4      = new Item(SecsFormat.F4, new float[0]);
        static readonly Item Empty_F8      = new Item(SecsFormat.F8, new double[0]);
        #endregion
    }
}