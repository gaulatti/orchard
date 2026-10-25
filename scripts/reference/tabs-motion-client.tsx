import React from 'react';
import {createRoot} from 'react-dom/client';
import {Tabs} from 'frozen/tabs';

const root = createRoot(document.getElementById('root')!);
window.showTabsMotion = spec => {
  const View = () => {
    const [active, setActive] = React.useState(spec.active ? 'activity' : 'overview');
    return <Tabs tabs={[{id: 'overview', label: 'Overview'}, {id: 'activity', label: 'Activity'}, {id: 'settings', label: 'Settings'}]}
      variant={spec.variant} activeTab={active} onChange={setActive}/>;
  };
  root.render(<View key={spec.id}/>);
};
