/** 
 * Copyright 2017 The Royal Veterinary College, jbullock AT rvc.ac.uk
 * 
 * This file is part of Folium.
 * 
 * Folium is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Folium is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Folium.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;

namespace Folium.Api.Extensions {
    public static class DbConnectionExtensions {
		// Adapted from https://stackoverflow.com/questions/6379155/multi-mapper-to-create-object-hierarchy/22415591#22415591
		public static async Task<IEnumerable<TParent>> QueryParentChildAsync<TParent, TChild, TParentKey, TChildReference>(
			this DbConnection connection,
			string sql,
			Func<TParent, TParentKey> parentKeySelector,
			Func<TParent, TChildReference> childSelector,
			Action<TChildReference, TChild> addChildToParentAction,
			dynamic param = null, 
			IDbTransaction transaction = null, 
			bool buffered = true, 
			string splitOn = "Id", 
			int? commandTimeout = null, 
			CommandType? commandType = null) {
			Dictionary<TParentKey, TParent> cache = new Dictionary<TParentKey, TParent>();

			await connection.QueryAsync<TParent, TChild, TParent>(
				sql,
				(parent, child) =>
				{
					if (!cache.ContainsKey(parentKeySelector(parent))) {
						cache.Add(parentKeySelector(parent), parent);
					}

					TParent cachedParent = cache[parentKeySelector(parent)];
					TChildReference children = childSelector(cachedParent);
					addChildToParentAction(children, child);
					return cachedParent;
				},
				param as object, transaction, buffered, splitOn, commandTimeout, commandType);

			return cache.Values;
		}
		public static async Task<IEnumerable<TParent>> QueryParentChildAsync<TParent, TChild, TParentKey>(
			this DbConnection connection,
			string sql,
			Func<TParent, TParentKey> parentKeySelector,
			Func<TParent, IList<TChild>> childSelector,
			dynamic param = null,
			IDbTransaction transaction = null,
			bool buffered = true,
			string splitOn = "Id",
			int? commandTimeout = null,
			CommandType? commandType = null) {
			return await DbConnectionExtensions.QueryParentChildAsync<TParent, TChild, TParentKey, IList<TChild>>(
				connection,
				sql,
				parentKeySelector,
				childSelector,
				(Action<IList<TChild>, TChild>)((childReference, child) => childReference.Add(child)),
				param,
				transaction,
				buffered,
				splitOn,
				commandTimeout,
				commandType);
		}
	}
}