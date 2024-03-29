// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;
using Hexa.NET.ImGui;

namespace Hexa.NET.ImGuizmo
{
	[StructLayout(LayoutKind.Sequential)]
	public partial struct Style
	{
		public float TranslationLineThickness;
		public float TranslationLineArrowSize;
		public float RotationLineThickness;
		public float RotationOuterLineThickness;
		public float ScaleLineThickness;
		public float ScaleLineCircleSize;
		public float HatchedAxisLineThickness;
		public float CenterCircleSize;
		public Vector4 Colors_0;
		public Vector4 Colors_1;
		public Vector4 Colors_2;
		public Vector4 Colors_3;
		public Vector4 Colors_4;
		public Vector4 Colors_5;
		public Vector4 Colors_6;
		public Vector4 Colors_7;
		public Vector4 Colors_8;
		public Vector4 Colors_9;
		public Vector4 Colors_10;
		public Vector4 Colors_11;
		public Vector4 Colors_12;
		public Vector4 Colors_13;
		public Vector4 Colors_14;

		public unsafe Span<Vector4> Colors
		
		{
			get
			{
				fixed (Vector4* p = &this.Colors_0)
				{
					return new Span<Vector4>(p, 15);
				}
			}
		}

		public unsafe void Destroy()
		{
			fixed (Style* @this = &this)
			{
				ImGuizmo.DestroyNative(@this);
			}
		}

	}

	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public unsafe struct StylePtr : IEquatable<StylePtr>
	{
		public StylePtr(Style* handle) { Handle = handle; }

		public Style* Handle;

		public bool IsNull => Handle == null;

		public static StylePtr Null => new StylePtr(null);

		public static implicit operator StylePtr(Style* handle) => new StylePtr(handle);

		public static implicit operator Style*(StylePtr handle) => handle.Handle;

		public static bool operator ==(StylePtr left, StylePtr right) => left.Handle == right.Handle;

		public static bool operator !=(StylePtr left, StylePtr right) => left.Handle != right.Handle;

		public static bool operator ==(StylePtr left, Style* right) => left.Handle == right;

		public static bool operator !=(StylePtr left, Style* right) => left.Handle != right;

		public bool Equals(StylePtr other) => Handle == other.Handle;

		/// <inheritdoc/>
		public override bool Equals(object obj) => obj is StylePtr handle && Equals(handle);

		/// <inheritdoc/>
		public override int GetHashCode() => ((nuint)Handle).GetHashCode();

		private string DebuggerDisplay => string.Format("StylePtr [0x{0}]", ((nuint)Handle).ToString("X"));
		public ref float TranslationLineThickness => ref Unsafe.AsRef<float>(&Handle->TranslationLineThickness);
		public ref float TranslationLineArrowSize => ref Unsafe.AsRef<float>(&Handle->TranslationLineArrowSize);
		public ref float RotationLineThickness => ref Unsafe.AsRef<float>(&Handle->RotationLineThickness);
		public ref float RotationOuterLineThickness => ref Unsafe.AsRef<float>(&Handle->RotationOuterLineThickness);
		public ref float ScaleLineThickness => ref Unsafe.AsRef<float>(&Handle->ScaleLineThickness);
		public ref float ScaleLineCircleSize => ref Unsafe.AsRef<float>(&Handle->ScaleLineCircleSize);
		public ref float HatchedAxisLineThickness => ref Unsafe.AsRef<float>(&Handle->HatchedAxisLineThickness);
		public ref float CenterCircleSize => ref Unsafe.AsRef<float>(&Handle->CenterCircleSize);
		public unsafe Span<Vector4> Colors
		
		{
			get
			{
				return new Span<Vector4>(&Handle->Colors_0, 15);
			}
		}

		public unsafe void Destroy()
		{
			ImGuizmo.DestroyNative(Handle);
		}

	}

}
