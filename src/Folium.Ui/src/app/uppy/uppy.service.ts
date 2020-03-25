import { Injectable } from '@angular/core';
import { Uppy } from '@uppy/core';
import { UppyPluginConfigurations } from './uppy/uppy.component';
import { SecurityService } from '../core/security.service';


@Injectable()
export class UppyService {
    readonly uppy = Uppy

  constructor(private securityService: SecurityService) { }

  configure(config: any, pluginConfig: UppyPluginConfigurations, uuid): any {
    const plugins = pluginConfig.map((plugin, i, all) => {
      if (plugin[0] == 'Dashboard') {
        plugin[2]['target'] = '.dashboard-container-' + uuid;
      }
      if (plugin[0] == 'Tus') {
        plugin[2]['headers'] = { 'Authorization': 'Bearer ' + this.securityService.authenticationToken };
      }
      return [plugin[1], plugin[2]]
    })

    var uppyInstance = new Uppy(config);

    plugins.forEach(function (plugin) {
      uppyInstance.use(plugin[0], plugin[1]);
    });

    return uppyInstance
  }
}
