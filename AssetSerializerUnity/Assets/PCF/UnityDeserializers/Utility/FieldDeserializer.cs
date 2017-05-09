using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace PCFFileFormat
{
	public class FieldDeserializer
	{
		private FieldInfo[] fields;
		private List<PropertySetter> customValueSetters;

		private System.Object parentObject;
		private Array array;
		private int index;

		public FieldDeserializer(FieldInfo[] fields, System.Object parentObject)
		{
			this.fields = fields;
			this.parentObject = parentObject;
			this.index = 0;
		}

		public FieldDeserializer(List<PropertySetter> customValueSetters, System.Object parentObject)
		{
			this.customValueSetters = customValueSetters;
			this.parentObject = parentObject;
			this.index = 0;
		}

		public FieldDeserializer(Array array)
		{
			this.array = array;
			this.index = 0;
		}

		public void SetField(string fieldName, System.Object value)
		{
			if (fields != null)
			{
				for (int i = 0; i < fields.Length; i++)
				{
					string name = fields[i].Name;
					Type fieldType = fields[i].FieldType;

					if (string.CompareOrdinal(name, fieldName) == 0)
					{
						if (fieldType.IsGenericType && typeof(IList).IsAssignableFrom(fields[i].FieldType))
						{
							//If this is a generic list and not an array we convert it.
							fields[i].SetValue(this.parentObject, Activator.CreateInstance(fieldType, value));
						}
						else
						{
							fields[i].SetValue(this.parentObject, value);
						}

						break;
					}
				}
			}
			else
			{
				if (customValueSetters != null)
				{
					for (int i = 0; i < customValueSetters.Count; i++)
					{
						string name = customValueSetters[i].GetFieldName();

						if (string.CompareOrdinal(name, fieldName) == 0)
						{
							customValueSetters[i].SetField(value);
							break;
						}
					}
				}
			}
		}

		public int SetArrayItem(System.Object value, int newIndex = -1)
		{
			if (array != null)
			{
                if (newIndex != -1)
                    array.SetValue(value, newIndex);
                else
                    array.SetValue(value, index);

                return index;
			}

            return 0;
		}

		public void IncrementIndex()
		{
			index++;
		}
	}
}