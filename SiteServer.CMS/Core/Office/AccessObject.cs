using System.Collections;
using BaiRong.Core;
using SiteServer.CMS.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Collections.Specialized;
using BaiRong.Core.Model.Enumerations;
using BaiRong.Core.Table;

namespace SiteServer.CMS.Core.Office
{
	public class AccessObject
	{
        public static bool CreateAccessFileForContents(string filePath, PublishmentSystemInfo publishmentSystemInfo, NodeInfo nodeInfo, List<int> contentIdList, List<string> displayAttributes, bool isPeriods, string dateFrom, string dateTo, ETriState checkedState)
        {
            DirectoryUtils.CreateDirectoryIfNotExists(DirectoryUtils.GetDirectoryPath(filePath));
            FileUtils.DeleteFileIfExists(filePath);

            var sourceFilePath = SiteServerAssets.GetPath(SiteServerAssets.Default.AccessMdb);
            FileUtils.CopyFile(sourceFilePath, filePath);

            var relatedidentityes = RelatedIdentities.GetChannelRelatedIdentities(publishmentSystemInfo.PublishmentSystemId, nodeInfo.NodeId);

            var tableName = NodeManager.GetTableName(publishmentSystemInfo, nodeInfo);
            var styleInfoList = TableStyleManager.GetTableStyleInfoList(tableName, relatedidentityes);
            styleInfoList = ContentUtility.GetAllTableStyleInfoList(publishmentSystemInfo, styleInfoList);

            var accessDao = new AccessDao(filePath);

            var createTableSqlString = accessDao.GetCreateTableSqlString(nodeInfo.NodeName, styleInfoList, displayAttributes);
            accessDao.ExecuteSqlString(createTableSqlString);

            bool isExport;

            var insertSqlArrayList = accessDao.GetInsertSqlStringArrayList(nodeInfo.NodeName, publishmentSystemInfo.PublishmentSystemId, nodeInfo.NodeId, tableName, styleInfoList, displayAttributes, contentIdList, isPeriods, dateFrom, dateTo, checkedState, out isExport);

            foreach (string insertSql in insertSqlArrayList)
            {
                accessDao.ExecuteSqlString(insertSql);
            }

            return isExport;
        }

        public static ArrayList GetContentsByAccessFile(string filePath, PublishmentSystemInfo publishmentSystemInfo, NodeInfo nodeInfo)
        {
            var contentInfoArrayList = new ArrayList();

            var accessDao = new AccessDao(filePath);
            var tableNames = accessDao.GetTableNames();
            if (tableNames != null && tableNames.Length > 0)
            {
                foreach (var tableName in tableNames)
                {
                    string sqlString = $"SELECT * FROM [{tableName}]";
                    var dataset = accessDao.ReturnDataSet(sqlString);

                    var oleDt = dataset.Tables[0];

                    if (oleDt.Rows.Count > 0)
                    {
                        var relatedidentityes = RelatedIdentities.GetChannelRelatedIdentities(publishmentSystemInfo.PublishmentSystemId, nodeInfo.NodeId);

                        var theTableName = NodeManager.GetTableName(publishmentSystemInfo, nodeInfo);

                        var tableStyleInfoList = TableStyleManager.GetTableStyleInfoList(theTableName, relatedidentityes);

                        var nameValueCollection = new NameValueCollection();

                        foreach (var styleInfo in tableStyleInfoList)
                        {
                            nameValueCollection[styleInfo.DisplayName] = styleInfo.AttributeName.ToLower();
                        }

                        var attributeNames = new ArrayList();
                        for (var i = 0; i < oleDt.Columns.Count; i++)
                        {
                            var columnName = oleDt.Columns[i].ColumnName;
                            attributeNames.Add(!string.IsNullOrEmpty(nameValueCollection[columnName])
                                ? nameValueCollection[columnName]
                                : columnName);
                        }

                        foreach (DataRow row in oleDt.Rows)
                        {
                            var contentInfo = new BackgroundContentInfo();

                            for (var i = 0; i < oleDt.Columns.Count; i++)
                            {
                                var attributeName = attributeNames[i] as string;
                                if (!string.IsNullOrEmpty(attributeName))
                                {
                                    var value = row[i].ToString();
                                    contentInfo.Set(attributeName, value);
                                }
                            }

                            if (!string.IsNullOrEmpty(contentInfo.Title))
                            {
                                contentInfo.PublishmentSystemId = publishmentSystemInfo.PublishmentSystemId;
                                contentInfo.NodeId = nodeInfo.NodeId;
                                contentInfo.LastEditDate = DateTime.Now;

                                contentInfoArrayList.Add(contentInfo);
                            }
                        }
                    }
                }
            }

            return contentInfoArrayList;
        }
	}
}
