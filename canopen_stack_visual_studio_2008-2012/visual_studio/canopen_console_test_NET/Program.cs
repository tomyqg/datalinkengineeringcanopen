﻿/*             _____        _        _      _       _    
              |  __ \      | |      | |    (_)     | |   
              | |  | | __ _| |_ __ _| |     _ _ __ | | __
              | |  | |/ _` | __/ _` | |    | | '_ \| |/ /
              | |__| | (_| | || (_| | |____| | | | |   < 
              |_____/ \__,_|\__\__,_|______|_|_| |_|_|\_\
         ______             _                      _             
        |  ____|           (_)                    (_)            
        | |__   _ __   __ _ _ _ __   ___  ___ _ __ _ _ __   __ _ 
        |  __| | '_ \ / _` | | '_ \ / _ \/ _ \ '__| | '_ \ / _` |
        | |____| | | | (_| | | | | |  __/  __/ |  | | | | | (_| |
        |______|_| |_|\__, |_|_| |_|\___|\___|_|  |_|_| |_|\__, |
                       __/ |                                __/ |
                      |___/                                |___/ 

      Web: http://www.datalink.se E-mail: ulrik.hagstrom@datalink.se

    *******************************************************************
    *    CANopen API (C++/C#) distributed by Datalink Enginnering.    *
    *             Copyright (C) 2009-2010 Ulrik Hagström.             *
    *******************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;



namespace canopen_demo_kvaser
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("******************************************************");
            Console.WriteLine("* CONNECT TWO CAN-ADAPTERS TO A SILENT CANBUS        *");
            Console.WriteLine("* AND THE TWO ADAPTERS WILL DO CANOPEN COMMUNICATION *");
            Console.WriteLine("* HIT RETURN 1 TIME WHEN DONE!                       *");
            Console.WriteLine("******************************************************");
            Console.ReadLine();

            byte[] test;
            uint uint_test;
            uint uint_temp_val;
            ushort ushort_temp_val;

            SDO_NET.CanOpenStatus stat;

            test = new byte[10];

            CanMonitor_NET can_Monitor;
            can_Monitor = new CanMonitor_NET();


            Console.WriteLine("**************************************************");
            Console.WriteLine("* Press RETURN to see some NMT protocol commands *");
            Console.WriteLine("**************************************************");
            Console.ReadLine();

            ServerSDO_NET server_SDO;
            server_SDO = new ServerSDO_NET();

            ClientSDO_NET client_SDO;
            client_SDO = new ClientSDO_NET();

            NMT_Master_NET nmt_Master;
            nmt_Master = new NMT_Master_NET();

            NMT_Slave_NET nmt_Slave1;
            nmt_Slave1 = new NMT_Slave_NET();

            NMT_Slave_NET nmt_Slave2;
            nmt_Slave2 = new NMT_Slave_NET();

            NMT_Slave_NET nmt_Slave3;
            nmt_Slave3 = new NMT_Slave_NET();


            can_Monitor.registerCanReceiveCallback((Object)can_Monitor, canMonitorCallback);
            if (can_Monitor.canHardwareConnect(1, 250000) != CANOPEN_LIB_ERROR.CanOpenStatus.CANOPEN_OK)
            {
                Console.WriteLine("Could not connect to CAN-adapter 1");
                Console.ReadLine();
            }


            

            nmt_Slave1.nodeSetId(3);
            nmt_Slave1.canHardwareConnect(1, 250000);
            nmt_Slave2.nodeSetId(5);
            nmt_Slave2.canHardwareConnect(1, 250000);
            nmt_Slave3.nodeSetId(66);
            nmt_Slave3.canHardwareConnect(1, 250000);


            if (nmt_Master.canHardwareConnect(0, 250000) != CANOPEN_LIB_ERROR.CanOpenStatus.CANOPEN_OK)
            {
                Console.WriteLine("Could not connect to CAN-adapter 0");
                Console.ReadLine();
            }

            nmt_Master.registerNodeStateCallback(new NMTOperationalStateDelegate(node_state_callback), (Object)nmt_Master);

            nmt_Master.nodeGuardPollStart(3, 3000);
            nmt_Master.nodeGuardPollStart(5, 3000);
            nmt_Master.nodeGuardPollStart(66, 3000);

            Thread.Sleep(2000);

            nmt_Master.nodeGuardPollStop(3); // Stopping the nodeguarding service to avoid too much Console-prints.
            nmt_Master.nodeGuardPollStop(5);
            nmt_Master.nodeGuardPollStop(66);
            Thread.Sleep(2000);
            
            Console.WriteLine("************************************************");
            Console.WriteLine("* Press return do EMERGENCY RX/TX              *");
            Console.WriteLine("************************************************");
            Console.ReadLine();


            EmcyClient_NET emcyClient = new EmcyClient_NET();
            emcyClient.canHardwareConnect(0, 250000);
            
            EmcyServer_NET emcyServer = new EmcyServer_NET();
            emcyServer.canHardwareConnect(1, 250000);

            /*
             * public delegate CANOPEN_LIB_ERROR.CanOpenStatus EmcyServerDelegate(
             *   object obj, byte nodeId, ushort emcyErrorCode, byte errorRegister, 
             *   byte[] manufacturerSpecificErrorField) 
             */

            emcyServer.registerEmcyServerMessageCallBack((object)emcyServer, emcy_server_callback);
            emcyClient.nodeSetId(5);
            emcyClient.sendEmcyMessage(0x1234, 0x10, new byte[] { 1, 2, 3, 4, 5 });

            Thread.Sleep(1000);
            /*
             * public delegate CANOPEN_LIB_ERROR.CanOpenStatus ReceivePdoDelegate(object obj, uint id, byte[] data, byte len)
             */

            Thread.Sleep(2000);
            Console.WriteLine("************************************************");
            Console.WriteLine("* Press return do PDO RX/TX                    *");
            Console.WriteLine("************************************************");
            Console.ReadLine();

            ReceivePDO_NET rPdo = new ReceivePDO_NET();
            rPdo.canHardwareConnect(1, 250000);
            rPdo.setCobid(0x123);
            rPdo.registerReceivePdoMessageCallBack((object)rPdo, receive_pdo_callback);


            TransmitPDO_NET tPdo = new TransmitPDO_NET();
            tPdo.canHardwareConnect(1, 250000);
            tPdo.setup(0x123, new byte[] { 1, 2, 3, 4, 5 }, 5);
            tPdo.periodicTransmission(true);

            Thread.Sleep(5000);
            tPdo.periodicTransmission(false);

            client_SDO.setReadObjectTimeout(10000);
            client_SDO.setWriteObjectTimeout(40000);
            client_SDO.setNodeResponseTimeout(10000);

            stat = client_SDO.canHardwareConnect(0, 250000);
            stat = client_SDO.connect(3);

            stat = server_SDO.canHardwareConnect(1, 250000);
            stat = server_SDO.nodeSetId(3);


            /***
             * Configure the callbacks. 
             ***/

            server_SDO.registerObjectReadCallback(new SrvReadDelegate(canopenReadCallback), (Object)server_SDO);
            server_SDO.registerObjectWriteCallback(new SrvWriteDelegate(canopenWriteCallback), (Object)server_SDO);
            server_SDO.registerObjectGetAttributesCallback(new SrvGetAttrDelegate(getAttributesCallback), (Object)server_SDO);

            Thread.Sleep(2000);
            Console.WriteLine("************************************************");
            Console.WriteLine("* Press return do EXPEITED READ (test 1/2)!    *");
            Console.WriteLine("************************************************");
            

            Console.ReadLine();
            client_SDO.setReadObjectTimeout(500000);
            client_SDO.setNodeResponseTimeout(10000);

            uint bytesWrittenByRemoteNode;
            stat = client_SDO.objectRead(0x0A, 0x0A, out uint_temp_val, out bytesWrittenByRemoteNode);
            if (stat == SDO_NET.CanOpenStatus.CANOPEN_OK)
            {
                if (uint_temp_val != 0x40302010 && bytesWrittenByRemoteNode != 4)
                {
                    Console.WriteLine("ERROR!");
                    Console.ReadLine();
                    return;
                }
            }

            Thread.Sleep(2000);
            Console.WriteLine("************************************************");
            Console.WriteLine("* Press return do EXPEITED READ (test 2/2)!    *");
            Console.WriteLine("************************************************");
            Console.ReadLine();

            client_SDO.setReadObjectTimeout(2000); // 2 seconds.
            stat = client_SDO.objectRead(0xB0, 0xB0, out ushort_temp_val, out uint_test);
            if (stat != SDO_NET.CanOpenStatus.CANOPEN_BUFFER_TOO_SMALL)
            {
                Console.WriteLine("ERROR!");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Yes, this is correct, we try to read 40 bytes into 4 bytes!");
                Console.WriteLine("Let's try with a bigger receive-buffer!");
            }

            Console.WriteLine("Waiting for SDO server to time out...");
            Thread.Sleep(7000);
            Console.WriteLine("************************************************");
            Console.WriteLine("* Press return do SEGEMNETED READ (40 bytes)!  *");
            Console.WriteLine("************************************************");
            Console.ReadLine();

            uint responseErrorCode;

            byte[] rx_buffer = new byte[2000];
            stat = client_SDO.objectRead(0xb0, 0xb0, rx_buffer, out bytesWrittenByRemoteNode, out responseErrorCode);
            if (stat != SDO_NET.CanOpenStatus.CANOPEN_OK && bytesWrittenByRemoteNode != 40)
            {
                Console.WriteLine("ERROR!");
                Console.ReadLine();
                return;
            }
            else
            {
                string received_data = string.Empty;
                Console.WriteLine(String.Format("Just received {0} bytes of data from remote node!", bytesWrittenByRemoteNode));

                for (int i = 0; i < bytesWrittenByRemoteNode; i++)
                    received_data += String.Format("{0:X2} ", rx_buffer[i]);

                Console.WriteLine(received_data);

            }

            Thread.Sleep(2000);
            Console.WriteLine("************************************************");
            Console.WriteLine("* Press return do SEGEMNETED READ (1024 bytes)! *");
            Console.WriteLine("************************************************");
            Console.ReadLine();

            stat = client_SDO.objectRead(0xc0, 0xc0, rx_buffer, out bytesWrittenByRemoteNode, out responseErrorCode);
            if (stat != SDO_NET.CanOpenStatus.CANOPEN_OK && bytesWrittenByRemoteNode != 1024)
            {
                Console.WriteLine("ERROR!");
                Console.ReadLine();
                return;
            }
            else
            {
                string received_data = string.Empty;
                Console.WriteLine(String.Format("Just received {0} bytes of data from remote node!", bytesWrittenByRemoteNode));

                for (int i = 0; i < bytesWrittenByRemoteNode; i++)
                    received_data += String.Format("{0:X2} ", rx_buffer[i]);

                Console.WriteLine(received_data);
            }

            Console.WriteLine("**********************************************************************");
            Console.WriteLine("* Press return do EXPEDITED WRITE TEST WITH EXPECTED ERROR RECEIVED! *");
            Console.WriteLine("**********************************************************************");
            Console.ReadLine();

            responseErrorCode = 0;
            if ((client_SDO.objectWrite(0xD0, 0xD0, (byte)0x55, out responseErrorCode)) == SDO_NET.CanOpenStatus.CANOPEN_REMOTE_NODE_ABORT)
            {
                Console.WriteLine(String.Format("Expected remote errorcode received, no write access to that object! {0:X4}", responseErrorCode));
            }

            Console.WriteLine("***************************************************");
            Console.WriteLine("* Press return do EXPEDITED WRITE TEST (1 bytes)! *");
            Console.WriteLine("***************************************************");
            Console.ReadLine();

            if ((client_SDO.objectWrite(0xE0, 0x00, (byte)0x55, out responseErrorCode)) != SDO_NET.CanOpenStatus.CANOPEN_OK)
            {
                Console.WriteLine("ERROR!");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("***************************************************");
            Console.WriteLine("* Press return do EXPEDITED WRITE TEST (2 bytes)! *");
            Console.WriteLine("***************************************************");
            Console.ReadLine();

            if ((client_SDO.objectWrite( 0xE0, 0x00, (ushort)0x1234, out responseErrorCode)) != SDO_NET.CanOpenStatus.CANOPEN_OK)
            {
                Console.WriteLine("ERROR!");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("***************************************************");
            Console.WriteLine("* Press return do EXPEDITED WRITE TEST (4 bytes)! *");
            Console.WriteLine("***************************************************");
            Console.ReadLine();

            if ((client_SDO.objectWrite( 0xE0, 0x00, (uint)0x12345678, out responseErrorCode)) != SDO_NET.CanOpenStatus.CANOPEN_OK)
            {
                Console.WriteLine("ERROR!");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("*****************************************************");
            Console.WriteLine("* Press return do SEGMENTED WRITE TEST (100 bytes)! *");
            Console.WriteLine("*****************************************************");
            Console.ReadLine();

            byte[] tx_buffer = new byte[2000];

            for (int i = 0; i < tx_buffer.Length; i++)
                tx_buffer[i] = (byte)i;

            if ((stat = client_SDO.objectWrite(0xE0, 0x00, tx_buffer, (ushort)100, out responseErrorCode)) != SDO_NET.CanOpenStatus.CANOPEN_OK)
            {
                Console.WriteLine("ERROR!");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("******************************************************");
            Console.WriteLine("* Press return do SEGMENTED WRITE TEST (10000 bytes)! *");
            Console.WriteLine("******************************************************");
            Console.ReadLine();

            tx_buffer = new byte[10000];

            for (int i = 0; i < tx_buffer.Length; i++)
                tx_buffer[i] = (byte)i;

            if ((stat = client_SDO.objectWrite(0xE0, 0x00, tx_buffer, (ushort)10000, out responseErrorCode)) != SDO_NET.CanOpenStatus.CANOPEN_OK)
            {
                Console.WriteLine("ERROR!");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("******************************************************");
            Console.WriteLine("* Press return do BLOCK WRITE TEST (10000 bytes)!    *");
            Console.WriteLine("******************************************************");
            Console.ReadLine();

            tx_buffer = new byte[10000];

            for (int i = 0; i < tx_buffer.Length; i++)
                tx_buffer[i] = (byte)i;


            if ((stat = client_SDO.objectWriteBlock( 0xE0, 0x00, 0, tx_buffer, (ushort)10000, out responseErrorCode)) != SDO_NET.CanOpenStatus.CANOPEN_OK)
            {
                Console.WriteLine("ERROR!");
                Console.ReadLine();
                return;
            }


        }

        static CanMonitor_NET.CanOpenStatus canMonitorCallback(object obj, uint id, byte[] data, byte dlc, uint flags)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.BackgroundColor = ConsoleColor.Black;

            string can_data = String.Empty;
            foreach (byte b in data)
                can_data += String.Format("{0:X2} ", b);

            Console.WriteLine(String.Format("CAN MONITOR CALLBACK: id: {0:X3}, dlc: {1}, data: {2}", id, dlc, can_data));

            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;

            return CANOPEN_LIB_ERROR.CanOpenStatus.CANOPEN_OK;
        }

        static SDO_NET.CanOpenStatus getAttributesCallback(object anyObject, ushort object_index, byte sub_index, out ushort flags)
        {
            flags = 0; // Nor R/W-accessable.
            switch (object_index)
            {
                case 0xA:
                    if (sub_index == 0xA)
                        flags = OBJECT_ATTRIBUTES_NET.OBJECT_READABLE;
                    break;
                case 0xB0:
                    if (sub_index == 0xB0)
                        flags = OBJECT_ATTRIBUTES_NET.OBJECT_READABLE;
                    break;
                case 0xC0:
                    if (sub_index == 0xC0)
                        flags = OBJECT_ATTRIBUTES_NET.OBJECT_READABLE;
                    break;
                case 0xD0:
                    if (sub_index == 0xD0)
                        flags = OBJECT_ATTRIBUTES_NET.OBJECT_READABLE;
                    break;
                case 0xE0:
                    if (sub_index == 0x00)
                        flags = OBJECT_ATTRIBUTES_NET.OBJECT_WRITEABLE;
                    break;
            }

            return SDO_NET.CanOpenStatus.CANOPEN_OK;
        }



        static SDO_NET.CanOpenStatus canopenReadCallback(object anyObject, ushort objectIndex, byte subIndex, byte[] data, out uint valid, out uint coErrorCode)
        {
            Console.BackgroundColor = ConsoleColor.Cyan;
            Console.WriteLine("CALLBACK: Read from Server SDO ObjIdx: {0:X2}, SubIdx: {1:X2} request!", objectIndex, subIndex);
            Console.BackgroundColor = ConsoleColor.Black;

            Console.Beep();
            valid = 0;
            coErrorCode = 0;

            if (objectIndex == 0xb0 && subIndex == 0xb0)
            {
                valid = 40;

                for (byte i = 0; i < valid; i++)
                    data[i] = i;

                coErrorCode = 0;

                return SDO_NET.CanOpenStatus.CANOPEN_OK;
            }

            if (objectIndex == 0xc0 && subIndex == 0xc0)
            {
                valid = 1024;

                for (int i = 0; i < valid; i++)
                    data[i] = (byte)i;

                coErrorCode = 0;

                return SDO_NET.CanOpenStatus.CANOPEN_OK;
            }

            // Expedited response on Object 10, sub 10.
            if (objectIndex == 10 && subIndex == 10)
            {
                valid = 4;
                data[0] = 0x10;
                data[1] = 0x20;
                data[2] = 0x30;
                data[3] = 0x40;
                coErrorCode = 0;
                return SDO_NET.CanOpenStatus.CANOPEN_OK;
            }
            return SDO_NET.CanOpenStatus.CANOPEN_OK;

        }


        static SDO_NET.CanOpenStatus canopenWriteCallback(object anyObject, ushort objectIndex, byte subIndex, byte[] data, uint valid, out uint coErrorCode)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.WriteLine("SDO SERVER CALLBACK: Write to Server SDO ObjIdx: {0:X2}, SubIdx: {1:X2} request!", objectIndex, subIndex);
            Console.BackgroundColor = ConsoleColor.Black;


            string objectData = String.Empty;
            for (int i = 0; i < valid; i++)
            {
                objectData += String.Format("0x{0:X2} ", data[i]);
            }
            Console.WriteLine(objectData);
            coErrorCode = 0;

            return SDO_NET.CanOpenStatus.CANOPEN_OK;
        }


        static NMT_Master_NET.CanOpenStatus node_state_callback(object any, byte node_id, byte state)
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.WriteLine("NMT CALLBACK: Node State result : node_id: {0}, state: {1}", node_id, state);
            Console.BackgroundColor = ConsoleColor.Black;
            return NMT_Master_NET.CanOpenStatus.CANOPEN_OK;
        }


        static CANOPEN_LIB_ERROR.CanOpenStatus emcy_server_callback(object obj, byte nodeId, ushort emcyErrorCode, byte errorRegister, byte[] manufacturerSpecificErrorField)
        {
            Console.WriteLine("EMERGENCY CALLBACK!");

            string manufErrorFiledString = string.Empty;
            foreach (byte b in manufacturerSpecificErrorField)
                manufErrorFiledString += String.Format("{0:X2} ", b);

            Console.WriteLine(String.Format("nodeId={0}\nemcyErrorCode=0x{1:X2}\nerrorRegister=0x{2:X1}, manufacturerSpecificErrorField={3}",
                nodeId, emcyErrorCode, errorRegister, manufErrorFiledString));

            return CANOPEN_LIB_ERROR.CanOpenStatus.CANOPEN_OK;
        }

        static CANOPEN_LIB_ERROR.CanOpenStatus receive_pdo_callback(object obj, uint id, byte[] data, byte len)
        {
            string rxPdoDataString = string.Empty;

            foreach (byte b in data)
                rxPdoDataString += String.Format("{0:X2} ", b);

            Console.WriteLine(String.Format("RPDO CALLBACK: COBID: {0:X3}, data: {1}, length: {2}", id, rxPdoDataString, len));
            return CANOPEN_LIB_ERROR.CanOpenStatus.CANOPEN_OK; ;
        }
    }
}