﻿using ProxyClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyServer
{
    public class ProxySettingsViewModel : BindableBase
    {
        private Int32 port = 8090;
        private Int32 cacheTimeout = 60000;
        private Int32 bufferSize = 1024;
        private Boolean checkModifiedContent = false;
        private Boolean contentFilterOn = false;
        private Boolean basicAuthOn = false;
        private Boolean allowChangeHeaders = false;
        private Boolean logRequestHeaders = false;
        private Boolean logContentIn = true;
        private Boolean logContentOut = true;
        private Boolean logCLientInfo = true;
        private Boolean serverRunning = false;
        public Int32 Port
        {
            get { return port; }
            set
            {
                if (SetProperty<int>(ref port, value)) this.port = value;
            }
        }
        public Int32 CacheTimeout
        {
            get { return cacheTimeout; }
            set
            {
                if (SetProperty<int>(ref cacheTimeout, value))
                {
                    this.cacheTimeout = value;
                }
            }
        }
        public Int32 BufferSize
        {
            get { return bufferSize; }
            set
            {
                if (value <= 0)
                {
                    value = 1;
                }
                if (SetProperty<int>(ref bufferSize, value) && value > 0)
                {
                    this.bufferSize = value;
                }
            }
        }
        public bool CheckModifiedContent
        {
            get { return checkModifiedContent; }
            set
            {
                if (SetProperty<bool>(ref checkModifiedContent, value))
                {
                    this.checkModifiedContent = value;
                }
            }
        }
        public bool ContentFilterOn
        {
            get { return contentFilterOn; }
            set
            {
                if (SetProperty<bool>(ref contentFilterOn, value))
                {
                    this.contentFilterOn = value;
                }
            }
        }
        public bool BasicAuthOn
        {
            get { return basicAuthOn; }
            set
            {
                if (SetProperty<bool>(ref basicAuthOn, value))
                {
                    this.basicAuthOn = value;
                }
            }
        }
        public bool AllowChangeHeaders
        {
            get { return allowChangeHeaders; }
            set
            {
                if (SetProperty<bool>(ref allowChangeHeaders, value))
                {
                    this.allowChangeHeaders = value;
                }
            }
        }

        public bool LogRequestHeaders
        {
            get { return logRequestHeaders; }
            set
            {
                if (SetProperty<bool>(ref logRequestHeaders, value))
                {
                    this.logRequestHeaders = value;
                }
            }
        }
        public bool LogContentIn
        {
            get { return logContentIn; }
            set
            {
                if (SetProperty<bool>(ref logContentIn, value))
                {
                    this.logContentIn = value;
                }
            }
        }
        public bool LogContentOut
        {
            get { return logContentOut; }
            set
            {
                if (SetProperty<bool>(ref logContentOut, value))
                {
                    this.logContentOut = value;
                }
            }
        }
        public bool LogCLientInfo
        {
            get { return logCLientInfo; }
            set
            {
                if (SetProperty<bool>(ref logCLientInfo, value))
                {
                    this.logCLientInfo = value;
                }
            }
        }
        public bool ServerRunning
        {
            get { return serverRunning; }
            set
            {
                if (SetProperty<bool>(ref serverRunning, value))
                {
                    this.serverRunning = value;
                }
            }
        }
    }
}
