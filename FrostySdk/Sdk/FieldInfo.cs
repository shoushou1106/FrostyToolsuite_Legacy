﻿using System;
using System.Text;
using Frosty.Sdk.Attributes;
using Frosty.Sdk.IO;
using Frosty.Sdk.Sdk.TypeInfos;

namespace Frosty.Sdk.Sdk;

internal class FieldInfo : IComparable
{
    public string GetName() => m_name;
    public TypeInfo GetTypeInfo() => TypeInfo.TypeInfoMapping[p_typeInfo];
    public int GetEnumValue() => (int)p_typeInfo;

    private string m_name;
    private uint m_nameHash;
    private TypeFlags m_flags;
    private ushort m_offset;
    private long p_typeInfo;
    
    public void Read(MemoryReader reader, uint classHash)
    {
        if (TypeInfo.HasNames)
        {
            m_name = reader.ReadNullTerminatedString();
        }
        if (TypeInfo.Version > 4)
        {
            m_nameHash = reader.ReadUInt();
        }
        m_flags = reader.ReadUShort();
        m_offset = reader.ReadUShort();

        p_typeInfo = reader.ReadLong();

        if (!TypeInfo.HasNames)
        {
            if (Strings.FieldHashes.ContainsKey(classHash) && Strings.FieldHashes[classHash].ContainsKey(m_nameHash))
            {
                m_name = Strings.FieldHashes[classHash][m_nameHash];
            }
            else if (Strings.StringHashes.ContainsKey(m_nameHash))
            {
                m_name = Strings.StringHashes[m_nameHash];
            }
            else
            {
                m_name = $"Field_{m_nameHash:x8}";
            }
        }
    }

    public void CreateField(StringBuilder sb)
    {
        TypeInfo type = GetTypeInfo();
        string typeName = type.GetName();
        TypeFlags flags = type.GetFlags();
        bool isClass = false;

        if (type is ClassInfo)
        {
            typeName = "PointerRef";
            isClass = true;
        }
        
        if (type is ArrayInfo arrayInfo)
        {
            type = arrayInfo.GetTypeInfo();
            typeName = type.GetName();
            if (type is ClassInfo)
            {
                typeName = "PointerRef";
                isClass = true;
            }
            typeName = $"List<{typeName}>";
            sb.AppendLine($"[{nameof(EbxArrayMetaAttribute)}({type.GetFlags()})]");
        }
        sb.AppendLine($"[{nameof(EbxFieldMetaAttribute)}({(int)flags}, {m_offset}, {(isClass ? $"typeof({type.GetName()})" : "null")})]");
        if (m_nameHash != 0)
        {
            sb.AppendLine($"[{nameof(NameHashAttribute)}({(int)m_nameHash})]");
        }
        
        sb.AppendLine($"private {typeName} _{m_name};");
    }

    public int CompareTo(object? obj)
    {
        return m_offset.CompareTo((obj as FieldInfo)!.m_offset);
    }
}

public class Test
{
    public int TestProp { get; set; }
    
    [EbxFieldMeta(TypeFlags.TypeEnum.Int32)]
    private int _TestProp;
}