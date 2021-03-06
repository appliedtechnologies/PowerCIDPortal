import { Component, ViewChild } from "@angular/core";
import { DxTreeViewComponent } from "devextreme-angular";
import DataSource from "devextreme/data/data_source";
import { DeploymentPath } from "src/app/shared/models/deploymentpath.model";
import { DeploymentPathEnvironment } from "src/app/shared/models/deploymentpathenvironment.model";
import { Environment } from "src/app/shared/models/environment.model";
import { DeploymentpathService } from "src/app/shared/services/deploymentpath.service";
import { EnvironmentService } from "src/app/shared/services/environment.service";
import { DeploymentPathEnvironmentService } from "src/app/shared/services/deploymentpathEnvironment.service";
import { confirm } from "devextreme/ui/dialog";
import {
  LayoutService,
  NotificationType,
} from "src/app/shared/services/layout.service";

@Component({
  selector: "app-deploymentpath",
  templateUrl: "./deploymentpath.component.html",
  styleUrls: ["./deploymentpath.component.css"],
  providers: [DeploymentpathService, EnvironmentService],
})
export class DeploymentpathComponent {
  dataSourceDeploymentPaths: DataSource;
  dataSourceEnvironments: DataSource;
  deploymentPaths: DeploymentPath[];
  environments: Environment[];

  deploymentPathIdToExpand: number;
  deploymentPathEnvironments: DeploymentPathEnvironment;
  isAddDeploymentPath: boolean;
  newDeploymentPath: DeploymentPath = {};

  @ViewChild("treeViewDeploymentpath")
  treeViewDeploymentpath: DxTreeViewComponent;
  @ViewChild("treeViewEnvironment") treeViewEnvironment: DxTreeViewComponent;

  constructor(
    private deploymentpathService: DeploymentpathService,
    private environmentService: EnvironmentService,
    private deploymentPathEnvironmentService: DeploymentPathEnvironmentService,
    private layoutService: LayoutService
  ) {
    this.onClickSaveDeploymentPath = this.onClickSaveDeploymentPath.bind(this);
    this.loadDeploymentPaths();
    this.dataSourceDeploymentPaths = new DataSource({
      store: this.deploymentpathService.getStore(),

      expand: [
        "CreatedByNavigation",
        "Environments",
        "DeploymentPathEnvironments",
        "DeploymentPathEnvironments.EnvironmentNavigation",
      ],
    });

    this.dataSourceEnvironments = new DataSource({
      store: this.environmentService.getStore(),
    });
  }

  onClickSaveDeploymentPath(e) {
    this.deploymentpathService
      .getStore()
      .insert(this.newDeploymentPath)
      .then(() => {
        this.isAddDeploymentPath = false;
        this.newDeploymentPath = {};
        this.loadDeploymentPaths();
      });
  }

  onClickAddDeploymentPath() {
    this.isAddDeploymentPath = true;
  }

  public onClickDeleteDeploymentPathOrStep(e: any, entry: any): void {
    let result: Promise<boolean>;
    if (entry.StepNumber === undefined)
      result = confirm(
        `Would you like to delete the deployment path "${entry.Name}"?`,
        "Delete Deployment Path"
      );
    else
      result = confirm(
        `Would you like to remove the "${entry.EnvironmentNavigation.Name}" environment from the deployment path?`,
        "Remove Environment"
      );
    result.then((dialogResult) => {
      if (dialogResult == true) {
        if (entry["EnvironmentNavigation"] === undefined) {
          //delete deploymentpath
          this.deploymentpathService
            .getStore()
            .remove(entry.Id)
            .then(() => {
              this.loadDeploymentPaths().then(() => {
                this.deploymentPathIdToExpand = entry.Id;
                this.layoutService.notify({
                  type: NotificationType.Success,
                  message: "Changes have been saved",
                  displayTime: 1000,
                });
              });
            });
        } else {
          //delete deploymentPathEnvironment
          this.deploymentPathEnvironmentService
            .getStore()
            .remove({
              DeploymentPath: entry.DeploymentPath,
              Environment: entry.Environment,
            })
            .then(() => {
              this.loadDeploymentPaths().then(() => {
                this.deploymentPathIdToExpand = entry.DeploymentPath;
                this.layoutService.notify({
                  type: NotificationType.Success,
                  message: "Changes have been saved",
                  displayTime: 1000,
                });
              });
            });
        }
      }
    });
  }

  onAdd(e) {
    if (e.fromComponent === e.toComponent && e.fromIndex === e.toIndex) {
      return;
    }
    const fromTreeView = this.getTreeView(e.fromData);
    const toTreeView = this.getTreeView(e.toData);

    const fromNode = this.findNode(fromTreeView, e.fromIndex);
    const toNode = this.findNode(toTreeView, this.calculateToIndex(e));
    if (toNode === null) {
      return;
    }
    if (fromNode.itemData.Id === toNode.itemData.Id) {
      return;
    }
    const toItems = toTreeView.option("items");
    const FromItemDataId = fromNode.itemData.Id;

    if (toNode.itemData.EnvironmentNavigation === undefined) {
      const fromItemCheck = toNode.itemData.Environments.find(
        (item) => item.Id == fromNode.itemData.Id
      );
      if (fromItemCheck != undefined) {
        return;
      }
      const ToItemDataId = toNode.itemData.Id;
      this.deploymentPathEnvironmentService
        .getStore()
        .insert({
          Environment: FromItemDataId,
          DeploymentPath: ToItemDataId,
          StepNumber: toNode.itemData.Environments.length + 1,
        })
        .then(() => {
          this.loadDeploymentPaths().then(
            () => (this.deploymentPathIdToExpand = ToItemDataId)
          );
        });
    } else {
      toItems.forEach((element) => {
        if (
          element.DeploymentPathEnvironments.length !=
          element.Environments.length
        ) {
          const ToItemDataId = element.Id;
          if (
            element.DeploymentPathEnvironments.findIndex(
              (t) => t.Environment == fromNode.itemData.Id
            ) != -1
          ) {
            return;
          }
          this.deploymentPathEnvironmentService
            .getStore()
            .insert({
              Environment: FromItemDataId,
              DeploymentPath: ToItemDataId,
              StepNumber:
                e.toIndex - toItems.findIndex((e) => e.Id == ToItemDataId),
            })

            .then(() => {
              this.loadDeploymentPaths().then(() => {
                this.deploymentPathIdToExpand = ToItemDataId;
                this.layoutService.notify({
                  type: NotificationType.Success,
                  message: "Changes have been saved",
                  displayTime: 1000,
                });
              });
            });
        }
      });
    }
  }

  onDragChange(e) {
    if (e.fromComponent === e.toComponent) {
      const fromNode = this.findNode(this.getTreeView(e.fromData), e.fromIndex);

      const toNode = this.findNode(
        this.getTreeView(e.toData),
        this.calculateToIndex(e)
      );

      if (toNode !== null && this.isChildNode(fromNode, toNode)) {
        //e.cancel = true;
      }
    }
  }

  onContentReadyTreeViewDeploymentPaths(e) {
    if (this.deploymentPathIdToExpand) {
      this.treeViewDeploymentpath.instance.expandItem(
        this.deploymentPathIdToExpand
      );
      this.deploymentPathIdToExpand = null;
    }
  }

  loadDeploymentPaths() {
    return this.deploymentpathService
      .getStore()
      .load({
        expand: [
          "CreatedByNavigation",
          "Environments",
          "DeploymentPathEnvironments",
          "DeploymentPathEnvironments.EnvironmentNavigation",
        ],
      })
      .then((data) => {
        let sortedDeploymentPaths: DeploymentPath[] = data;

        let deploymentPathEnvironments: DeploymentPathEnvironment[] = [];

        for (let i = 0; i < data.length; i++) {
          for (let j = 0; j < data[i].DeploymentPathEnvironments.length; j++) {
            let stepNumber = data[i].DeploymentPathEnvironments[j].StepNumber;
            deploymentPathEnvironments[stepNumber - 1] =
              data[i].DeploymentPathEnvironments[j];
          }
          sortedDeploymentPaths[i].DeploymentPathEnvironments =
            deploymentPathEnvironments;
          deploymentPathEnvironments = [];
        }

        this.deploymentPaths = sortedDeploymentPaths;
      });
  }

  onReorder(e) {
    if (e.fromComponent === e.toComponent && e.fromIndex === e.toIndex) {
      return;
    }
    const fromTreeView = this.getTreeView(e.fromData);
    const toTreeView = this.getTreeView(e.toData);

    const fromNode = this.findNode(fromTreeView, e.fromIndex);
    const toNode = this.findNode(toTreeView, this.calculateToIndex(e));

    const toItems = toTreeView.option("items");

    let environmentId = fromNode.itemData.Environment;
    let deploymentPathId = fromNode.itemData.DeploymentPath;
    let fromIndex = fromNode.itemData.StepNumber;
    let toIndex = toNode.itemData.StepNumber;

    this.deploymentPathEnvironmentService
      .getStore()
      .update(
        {
          Environment: environmentId,
          DeploymentPath: deploymentPathId,
        },
        { ToIndex: toIndex, FromIndex: fromIndex }
      )
      .then(() => {
        this.loadDeploymentPaths().then(() => {
          this.deploymentPathIdToExpand = deploymentPathId;
          this.layoutService.notify({
            type: NotificationType.Success,
            message: "Changes have been saved",
            displayTime: 1000,
          });
        });
      });
  }
  onDragEnd(e) {
    if (e.fromComponent === e.toComponent && e.fromIndex === e.toIndex) {
      return;
    }

    const fromTreeView = this.getTreeView(e.fromData);
    const toTreeView = this.getTreeView(e.toData);
    const fromTopVisibleNode = this.getTopVisibleNode(e.fromComponent);
    const toTopVisibleNode = this.getTopVisibleNode(e.toComponent);

    const fromItems = fromTreeView.option("items");
    const toItems = toTreeView.option("items");

    const fromNode = this.findNode(fromTreeView, e.fromIndex);
    const toNode = this.findNode(toTreeView, this.calculateToIndex(e));

    if (e.dropInsideItem && toNode !== null && !toNode.itemData.isDirectory) {
      return;
    }

    this.moveNode(fromNode, toNode, fromItems, toItems, e.dropInsideItem);

    fromTreeView.option("items", fromItems);
    toTreeView.option("items", toItems);
    fromTreeView.scrollToItem(fromTopVisibleNode);
    toTreeView.scrollToItem(toTopVisibleNode);
  }

  findNode(treeView, index) {
    const nodeElement = treeView
      .element()
      .querySelectorAll(".dx-treeview-node")[index];
    if (nodeElement) {
      return this.findNodeById(
        treeView.getNodes(),
        nodeElement.getAttribute("data-item-id")
      );
    }
    return null;
  }

  findNodeById(nodes, id) {
    for (var i = 0; i < nodes.length; i++) {
      if (nodes[i].itemData.Id == id) {
        return nodes[i];
      }
      if (nodes[i].children) {
        const node = this.findNodeById(nodes[i].children, id);
        if (node != null) {
          return node;
        }
      }
    }
    return null;
  }

  moveNode(fromNode, toNode, fromItems, toItems, isDropInsideItem) {
    const fromNodeContainingArray = this.getNodeContainingArray(
      fromNode,
      fromItems
    );
    if (fromItems === toItems) {
      const fromIndex = fromNodeContainingArray.findIndex(
        (item) => item.Id == fromNode.itemData.Environment
      );
      fromNodeContainingArray.splice(fromIndex, 1);
    }

    if (isDropInsideItem) {
      toNode.itemData.items.splice(
        toNode.itemData.items.length,
        0,
        fromNode.itemData
      );
    } else {
      const toNodeContainingArray = this.getNodeContainingArray(
        toNode,
        toItems
      );

      if (toNodeContainingArray === toItems) {
        return;
      }
      const toIndex =
        toNode === null
          ? toNodeContainingArray.length
          : toNodeContainingArray.findIndex(
              (item) => item.Id == toNode.itemData.Environment
            );

      if (
        fromNode.itemData.EnvironmentNavigation === undefined &&
        toNode.itemData.Id != fromNode.itemData.Id
      ) {
        toNodeContainingArray.splice(toIndex, 0, fromNode.itemData);
        toNodeContainingArray;
      } else {
        toNodeContainingArray.splice(
          toIndex,
          0,
          fromNode.itemData.EnvironmentNavigation
        );
      }
    }
  }

  getNodeContainingArray(node, rootArray) {
    if (node === null || node.parent === null) return rootArray;
    else return node.parent.itemData.Environments;
  }

  isChildNode(parentNode, childNode) {
    let parent = childNode.parent;
    while (parent !== null) {
      if (parent.itemData.id === parentNode.itemData.id) {
        return true;
      }
      parent = parent.parent;
    }
    return false;
  }

  getTopVisibleNode(component) {
    const treeViewElement = component.element();
    const treeViewTopPosition = treeViewElement.getBoundingClientRect().top;
    const nodes = treeViewElement.querySelectorAll(".dx-treeview-node");
    for (let i = 0; i < nodes.length; i++) {
      const nodeTopPosition = nodes[i].getBoundingClientRect().top;
      if (nodeTopPosition >= treeViewTopPosition) {
        return nodes[i];
      }
    }

    return null;
  }

  getTreeView(name) {
    return name === "deploymentpath"
      ? this.treeViewDeploymentpath.instance
      : this.treeViewEnvironment.instance;
  }

  calculateToIndex(e) {
    if (e.fromComponent != e.toComponent || e.dropInsideItem) {
      return e.toIndex;
    }

    return e.fromIndex >= e.toIndex ? e.toIndex : e.toIndex + 1;
  }
}
