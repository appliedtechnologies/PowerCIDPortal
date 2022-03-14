export var navigation = [
  {
    id: "user",
    text: "Home",
    icon: "home",
    path: "/",
    visible: true,
  },
  {
    id:"user",
    text: "Solutions Overview",
    icon: "at-icon powercid-icon-code-file",
    path: "/solutions",
    visible: false,
  },
  {
    id:"user",
    text: "Deployment History",
    icon: "at-icon powercid-icon-vergangenheit",
    path: "/history",
    visible: false,
  },
  {
    id:"manager",
    text: 'Workspace',
    icon: 'folder',
    visible: false,
    items:[
      {
        id:"manager",
        text: "Applications",
        icon: "at-icon powercid-icon-web-design",
        path: "/applications",
        visible: false,
      },
      {
        id:"manager",
        text: "Environments",
        icon: "at-icon powercid-icon-unit",
        path: "/environments",
        visible: false,
      },
      {
        id:"manager",
        text: "Deployment Paths",
        icon: "at-icon powercid-icon-route",
        path: "/deploymentpaths",
        visible: false,
      }
    ]
  },
  {
    id:"admin",
    text: 'App Settings',
    icon: 'folder',
    visible: false,
    items:[
        {
          id:"admin",
          text: "Users",
          icon: "at-icon powercid-icon-conference-hintergrund-ausgew-hlte",
          path: "/users",
          visible: false,
        },
        {
          id:"admin",
          text: "Settings",
          icon: "at-icon powercid-icon-einstellungen",
          path: "/settings",
          visible: false,
        }
      ]
    },
  ];
  