﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Sparrow.CommonLibrary.Database.Configuration
{
    public class BuilderElement : ConfigurationElement
    {
        [ConfigurationProperty("type", IsRequired = true)]
        [TypeConverter(typeof(TypeNameConverter))]
        [ConfigurationValidator(typeof(ConfigurationISqlBuilderTypeValidator))]
        public Type Type
        {
            get { return (Type)this["type"]; }
            set { this["type"] = value; }
        }
    }
}
