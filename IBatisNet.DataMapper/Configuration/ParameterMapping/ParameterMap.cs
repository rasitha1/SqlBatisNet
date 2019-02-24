#region Apache Notice

/*****************************************************************************
 * $Header: $
 * $Revision: 410610 $
 * $Date: 2006-05-31 19:40:07 +0200 (mer., 31 mai 2006) $
 * 
 * iBATIS.NET Data Mapper
 * Copyright (C) 2004 - Gilles Bayon
 *  
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *      http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 ********************************************************************************/

#endregion

#region Using

using System;
using System.Collections;
using System.Data;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using IBatisNet.Common.Logging;
using IBatisNet.DataMapper.Configuration.Serializers;
using IBatisNet.DataMapper.DataExchange;
using IBatisNet.DataMapper.Scope;
using IBatisNet.DataMapper.TypeHandlers;

#endregion

namespace IBatisNet.DataMapper.Configuration.ParameterMapping
{
    /// <summary>
    ///     Summary description for ParameterMap.
    /// </summary>
    [Serializable]
    [XmlRoot("parameterMap", Namespace = "http://ibatis.apache.org/mapping")]
    public class ParameterMap
    {
        /// <summary>
        ///     Token for xml path to parameter elements.
        /// </summary>
        private const string XML_PARAMATER = "parameter";

        #region Constructor (s) / Destructor

        /// <summary>
        ///     Do not use direclty, only for serialization.
        /// </summary>
        /// <param name="dataExchangeFactory"></param>
        public ParameterMap(DataExchangeFactory dataExchangeFactory)
        {
            _dataExchangeFactory = dataExchangeFactory;
        }

        #endregion

        #region private

        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [NonSerialized] private string _id = string.Empty;

        [NonSerialized]
        // Properties list
        private readonly ParameterPropertyCollection _properties = new ParameterPropertyCollection();

        // Same list as _properties but without doubled (Test UpdateAccountViaParameterMap2)
        [NonSerialized]
        private readonly ParameterPropertyCollection _propertiesList = new ParameterPropertyCollection();

        //(property Name, property)
        [NonSerialized]
        private readonly Hashtable
            _propertiesMap = new Hashtable(); // Corrected ?? Support Request 1043181, move to HashTable

        [NonSerialized] private string _extendMap = string.Empty;

        [NonSerialized] private bool _usePositionalParameters;

        [NonSerialized] private string _className = string.Empty;

        [NonSerialized] private Type _parameterClass;

        [NonSerialized] private readonly DataExchangeFactory _dataExchangeFactory;

        [NonSerialized] private IDataExchange _dataExchange;

        #endregion

        #region Properties

        /// <summary>
        ///     The parameter class name.
        /// </summary>
        [XmlAttribute("class")]
        public string ClassName
        {
            get => _className;
            set
            {
                if (_logger.IsInfoEnabled)
                    if ((value == null) || (value.Length < 1))
                        _logger.Info(
                            "The class attribute is recommended for better performance in a ParameterMap tag '" + _id +
                            "'.");


                _className = value;
            }
        }

        /// <summary>
        ///     The parameter type class.
        /// </summary>
        [XmlIgnore]
        public Type Class
        {
            set => _parameterClass = value;
            get => _parameterClass;
        }

        /// <summary>
        ///     Identifier used to identify the ParameterMap amongst the others.
        /// </summary>
        [XmlAttribute("id")]
        public string Id
        {
            get => _id;
            set
            {
                if ((value == null) || (value.Length < 1))
                    throw new ArgumentNullException("The id attribute is mandatory in a ParameterMap tag.");

                _id = value;
            }
        }


        /// <summary>
        ///     The collection of ParameterProperty
        /// </summary>
        [XmlIgnore]
        public ParameterPropertyCollection Properties => _properties;

        /// <summary>
        /// </summary>
        [XmlIgnore]
        public ParameterPropertyCollection PropertiesList => _propertiesList;

        /// <summary>
        ///     Extend Parametermap attribute
        /// </summary>
        /// <remarks>The id of a ParameterMap</remarks>
        [XmlAttribute("extends")]
        public string ExtendMap
        {
            get => _extendMap;
            set => _extendMap = value;
        }

        /// <summary>
        ///     Sets the IDataExchange
        /// </summary>
        [XmlIgnore]
        public IDataExchange DataExchange
        {
            set => _dataExchange = value;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Get the ParameterProperty at index.
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>A ParameterProperty</returns>
        public ParameterProperty GetProperty(int index)
        {
            if (_usePositionalParameters) //obdc/oledb
                return _properties[index];
            return _propertiesList[index];
        }

        /// <summary>
        ///     Get a ParameterProperty by his name.
        /// </summary>
        /// <param name="name">The name of the ParameterProperty</param>
        /// <returns>A ParameterProperty</returns>
        public ParameterProperty GetProperty(string name)
        {
            return (ParameterProperty) _propertiesMap[name];
        }


        /// <summary>
        ///     Add a ParameterProperty to the ParameterProperty list.
        /// </summary>
        /// <param name="property"></param>
        public void AddParameterProperty(ParameterProperty property)
        {
            // These mappings will replace any mappings that this map 
            // had for any of the keys currently in the specified map. 
            _propertiesMap[property.PropertyName] = property;
            _properties.Add(property);

            if (_propertiesList.Contains(property) == false) _propertiesList.Add(property);
        }

        /// <summary>
        ///     Insert a ParameterProperty ine the ParameterProperty list at the specified index..
        /// </summary>
        /// <param name="index">
        ///     The zero-based index at which ParameterProperty should be inserted.
        /// </param>
        /// <param name="property">The ParameterProperty to insert. </param>
        public void InsertParameterProperty(int index, ParameterProperty property)
        {
            // These mappings will replace any mappings that this map 
            // had for any of the keys currently in the specified map. 
            _propertiesMap[property.PropertyName] = property;
            _properties.Insert(index, property);

            if (_propertiesList.Contains(property) == false) _propertiesList.Insert(index, property);
        }

        /// <summary>
        ///     Retrieve the index for array property
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public int GetParameterIndex(string propertyName)
        {
            int idx = -1;
            //idx = (Integer) parameterMappingIndex.get(propertyName);
            idx = Convert.ToInt32(propertyName.Replace("[", "").Replace("]", ""));
            return idx;
        }


        /// <summary>
        ///     Get all Parameter Property Name
        /// </summary>
        /// <returns>A string array</returns>
        public string[] GetPropertyNameArray()
        {
            string[] propertyNameArray = new string[_propertiesMap.Count];

            for (int index = 0; index < _propertiesList.Count; index++)
                propertyNameArray[index] = _propertiesList[index].PropertyName;
            return propertyNameArray;
        }


        /// <summary>
        ///     Set parameter value, replace the null value if any.
        /// </summary>
        /// <param name="mapping"></param>
        /// <param name="dataParameter"></param>
        /// <param name="parameterValue"></param>
        public void SetParameter(ParameterProperty mapping, IDataParameter dataParameter, object parameterValue)
        {
            object value = _dataExchange.GetData(mapping, parameterValue);

            ITypeHandler typeHandler = mapping.TypeHandler;

            // Apply Null Value
            if (mapping.HasNullValue)
                if (typeHandler.Equals(value, mapping.NullValue))
                    value = null;

            typeHandler.SetParameter(dataParameter, value, mapping.DbType);
        }

        /// <summary>
        ///     Set output parameter value.
        /// </summary>
        /// <param name="mapping"></param>
        /// <param name="dataBaseValue"></param>
        /// <param name="target"></param>
        public void SetOutputParameter(ref object target, ParameterProperty mapping, object dataBaseValue)
        {
            _dataExchange.SetData(ref target, mapping, dataBaseValue);
        }

        #region Configuration

        /// <summary>
        ///     Initialize the parameter properties child.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="usePositionalParameters"></param>
        public void Initialize(bool usePositionalParameters, IScope scope)
        {
            _usePositionalParameters = usePositionalParameters;
            if (_className.Length > 0)
            {
                _parameterClass = _dataExchangeFactory.TypeHandlerFactory.GetType(_className);
                _dataExchange = _dataExchangeFactory.GetDataExchangeForClass(_parameterClass);
            }
            else
            {
                // Get the ComplexDataExchange
                _dataExchange = _dataExchangeFactory.GetDataExchangeForClass(null);
            }
        }


        /// <summary>
        ///     Build the properties
        /// </summary>
        /// <param name="scope"></param>
        public void BuildProperties(ConfigurationScope scope)
        {
            GetProperties(scope);
        }

        /// <summary>
        ///     Get the parameter properties child for the xmlNode parameter.
        /// </summary>
        /// <param name="configScope"></param>
        private void GetProperties(ConfigurationScope configScope)
        {
            ParameterProperty property = null;

            foreach (XmlNode parameterNode in configScope.NodeContext.SelectNodes(
                DomSqlMapBuilder.ApplyMappingNamespacePrefix(XML_PARAMATER), configScope.XmlNamespaceManager))
            {
                property = ParameterPropertyDeSerializer.Deserialize(parameterNode, configScope);

                property.Initialize(configScope, _parameterClass);

                AddParameterProperty(property);
            }
        }

        #endregion

        #endregion
    }
}